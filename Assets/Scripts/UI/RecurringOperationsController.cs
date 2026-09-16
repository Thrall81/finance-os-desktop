using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Binds RecurringOperations.uxml: list, creation (expense/income/savings transfer/internal
    /// transfer), and — in edit mode — everything except name and start date (type, accounts,
    /// expected amount, frequency, day-of-month, category, counterparty — see
    /// RecurringOperationService.UpdateOperation, ADR-140). Start date stays read-only because
    /// it's immutable in Domain by design; name has simply never had a reported need to change.
    /// See docs/07-Interface.md §3/§9/§10.
    /// </summary>
    public sealed class RecurringOperationsController
    {
        private static readonly (RecurringOperationType Type, string Text)[] TypeOptions =
        {
            (RecurringOperationType.Expense, "Dépense"),
            (RecurringOperationType.Income, "Revenu"),
            (RecurringOperationType.SavingsTransfer, "Virement épargne"),
            (RecurringOperationType.InternalTransfer, "Virement interne"),
        };

        private static readonly (RecurringFrequency Frequency, string Text)[] FrequencyOptions =
        {
            (RecurringFrequency.Weekly, "Hebdomadaire"),
            (RecurringFrequency.Monthly, "Mensuelle"),
            (RecurringFrequency.Bimonthly, "Bimestrielle"),
            (RecurringFrequency.Quarterly, "Trimestrielle"),
            (RecurringFrequency.Semiannual, "Semestrielle"),
            (RecurringFrequency.Yearly, "Annuelle"),
        };

        private readonly AccountService _accounts;
        private readonly CategoryService _categories;
        private readonly CounterpartyService _counterparties;
        private readonly RecurringOperationService _operations;
        private readonly AppSettingsService _settings;

        private readonly Button _newOperationButton;
        private readonly Button _newOperationButtonList;

        private readonly VisualElement _formCard;
        private readonly Label _formTitle;
        private readonly VisualElement _nameRow;
        private readonly TextField _nameField;
        private readonly VisualElement _typeRow;
        private readonly DropdownField _typeField;
        private readonly VisualElement _sourceAccountRow;
        private readonly Label _sourceAccountLabel;
        private readonly DropdownField _sourceAccountField;
        private readonly VisualElement _destinationAccountRow;
        private readonly Label _destinationAccountLabel;
        private readonly DropdownField _destinationAccountField;
        private readonly TextField _amountField;
        private readonly VisualElement _frequencyRow;
        private readonly DropdownField _frequencyField;
        private readonly VisualElement _startDateRow;
        private readonly TextField _startDateField;
        private readonly VisualElement _startDateReadonlyRow;
        private readonly Label _startDateReadonlyLabel;
        private readonly VisualElement _dayOfMonthRow;
        private readonly TextField _dayOfMonthField;
        private readonly VisualElement _categoryRow;
        private readonly DropdownField _categoryField;
        private readonly VisualElement _counterpartyRow;
        private readonly TextField _counterpartyField;
        private readonly Label _skipWarningLabel;
        private readonly Label _errorLabel;
        private readonly Button _deleteButton;
        private readonly Button _toggleActiveButton;
        private readonly Button _cancelButton;
        private readonly Button _submitButton;

        private readonly Label _emptyLabel;
        private readonly MultiColumnListView _listView;

        private IReadOnlyList<DropdownOption> _creatableAccounts = Array.Empty<DropdownOption>();
        private IReadOnlyList<DropdownOption> _categoryOptions = Array.Empty<DropdownOption>();
        private List<RecurringOperationRowViewModel> _rows = new();

        private int? _editingOperationId;
        private bool _editingIsActive;

        public RecurringOperationsController(
            VisualElement root,
            AccountService accounts,
            CategoryService categories,
            CounterpartyService counterparties,
            RecurringOperationService operations,
            AppSettingsService settings)
        {
            _accounts = accounts;
            _categories = categories;
            _counterparties = counterparties;
            _operations = operations;
            _settings = settings;

            _newOperationButton = root.Q<Button>("new-operation-button");
            _newOperationButtonList = root.Q<Button>("new-operation-button-list");

            _formCard = root.Q<VisualElement>("operation-form-card");
            _formTitle = root.Q<Label>("form-title");
            _nameRow = root.Q<VisualElement>("form-name-row");
            _nameField = root.Q<TextField>("form-name");
            _typeRow = root.Q<VisualElement>("form-type-row");
            _typeField = root.Q<DropdownField>("form-type");
            _sourceAccountRow = root.Q<VisualElement>("form-source-account-row");
            _sourceAccountLabel = root.Q<Label>("form-source-account-label");
            _sourceAccountField = root.Q<DropdownField>("form-source-account");
            _destinationAccountRow = root.Q<VisualElement>("form-destination-account-row");
            _destinationAccountLabel = root.Q<Label>("form-destination-account-label");
            _destinationAccountField = root.Q<DropdownField>("form-destination-account");
            _amountField = root.Q<TextField>("form-amount");
            NumericInputFilter.RestrictToDecimal(_amountField);
            _frequencyRow = root.Q<VisualElement>("form-frequency-row");
            _frequencyField = root.Q<DropdownField>("form-frequency");
            _startDateRow = root.Q<VisualElement>("form-start-date-row");
            _startDateField = root.Q<TextField>("form-start-date");
            _startDateReadonlyRow = root.Q<VisualElement>("form-start-date-readonly-row");
            _startDateReadonlyLabel = root.Q<Label>("form-start-date-readonly");
            _dayOfMonthRow = root.Q<VisualElement>("form-day-of-month-row");
            _dayOfMonthField = root.Q<TextField>("form-day-of-month");
            NumericInputFilter.RestrictToInteger(_dayOfMonthField);
            _categoryRow = root.Q<VisualElement>("form-category-row");
            _categoryField = root.Q<DropdownField>("form-category");
            _counterpartyRow = root.Q<VisualElement>("form-counterparty-row");
            _counterpartyField = root.Q<TextField>("form-counterparty");
            _skipWarningLabel = root.Q<Label>("form-skip-warning");
            _errorLabel = root.Q<Label>("form-error");
            _deleteButton = root.Q<Button>("form-delete-button");
            _toggleActiveButton = root.Q<Button>("form-toggle-active-button");
            _cancelButton = root.Q<Button>("form-cancel-button");
            _submitButton = root.Q<Button>("form-submit-button");

            _emptyLabel = root.Q<Label>("operations-empty");
            _listView = root.Q<MultiColumnListView>("operations-list-view");
            TableHeaderTheme.Wire(_listView, settings.Get().Theme == AppTheme.Dark);

            _typeField.choices = TypeOptions.Select(o => o.Text).ToList();
            _frequencyField.choices = FrequencyOptions.Select(o => o.Text).ToList();

            SetupColumns();

            _newOperationButton.clicked += OpenCreateForm;
            // Same action duplicated next to the list itself — see AccountsController's
            // identical wiring and ADR-142 for why.
            _newOperationButtonList.clicked += OpenCreateForm;
            _cancelButton.clicked += CloseForm;
            _submitButton.clicked += SubmitForm;
            _deleteButton.clicked += DeleteOperation;
            _toggleActiveButton.clicked += ToggleActive;
            _typeField.RegisterValueChangedCallback(evt => ApplyTypeVisibility(evt.newValue));
            _startDateField.RegisterValueChangedCallback(_ => UpdateSkipWarning());
            _dayOfMonthField.RegisterValueChangedCallback(_ => UpdateSkipWarning());
            _frequencyField.RegisterValueChangedCallback(_ => UpdateSkipWarning());
            _listView.selectionChanged += _ => OnRowSelected();

            Refresh();
        }

        private void SetupColumns()
        {
            _listView.columns.Add(BuildColumn("name", "Nom", r => r.Name, width: 160, minWidth: 100, stretchable: true));
            _listView.columns.Add(BuildColumn("type", "Type", r => r.TypeText, width: 110, minWidth: 90));
            _listView.columns.Add(BuildColumn("account", "Compte", r => r.AccountText, width: 170, minWidth: 110));
            _listView.columns.Add(BuildColumn("frequency", "Fréquence", r => r.FrequencyText, width: 110, minWidth: 90));
            _listView.columns.Add(BuildColumn("amount", "Montant", r => r.AmountText, width: 110, minWidth: 90, alignRight: true));
            _listView.columns.Add(BuildColumn("status", "Statut", r => r.IsActive ? "Actif" : "Suspendu", width: 90, minWidth: 80));
            _listView.selectionType = SelectionType.Single;
            _listView.fixedItemHeight = 28;
        }

        private Column BuildColumn(
            string name, string title, Func<RecurringOperationRowViewModel, string> textSelector,
            float width, float minWidth, bool alignRight = false, bool stretchable = false)
        {
            return new Column
            {
                name = name,
                title = title,
                width = width,
                minWidth = minWidth,
                stretchable = stretchable,
                makeCell = () =>
                {
                    var label = new Label();
                    if (alignRight)
                    {
                        label.AddToClassList("account-row-balance");
                    }

                    return label;
                },
                bindCell = (element, index) => ((Label)element).text = textSelector(_rows[index]),
            };
        }

        public void Refresh()
        {
            var viewModel = RecurringOperationsViewModelBuilder.Build(_accounts, _categories, _counterparties, _operations);

            _creatableAccounts = viewModel.Accounts;
            _categoryOptions = viewModel.Categories;
            _rows = viewModel.Operations.ToList();

            _newOperationButton.SetEnabled(_creatableAccounts.Count > 0);
            _newOperationButtonList.SetEnabled(_creatableAccounts.Count > 0);

            var hasRows = _rows.Count > 0;
            _emptyLabel.style.display = hasRows ? DisplayStyle.None : DisplayStyle.Flex;
            _listView.style.display = hasRows ? DisplayStyle.Flex : DisplayStyle.None;
            _listView.itemsSource = _rows;
            // Rebuild, not just RefreshItems — see TransactionsController.Refresh for why (ADR-116).
            _listView.Rebuild();
        }

        private void OnRowSelected()
        {
            var index = _listView.selectedIndex;
            if (index < 0 || index >= _rows.Count)
            {
                return;
            }

            OpenEditForm(_rows[index]);
        }

        private void OpenCreateForm()
        {
            _editingOperationId = null;

            _formTitle.text = "Nouvelle opération";

            _nameRow.style.display = DisplayStyle.Flex;
            _nameField.SetValueWithoutNotify(string.Empty);

            _typeRow.style.display = DisplayStyle.Flex;
            _typeField.SetValueWithoutNotify(TypeOptions[0].Text);

            SetChoices(_sourceAccountField, _creatableAccounts);
            SetChoices(_destinationAccountField, _creatableAccounts);

            _amountField.SetValueWithoutNotify(string.Empty);

            _frequencyRow.style.display = DisplayStyle.Flex;
            _frequencyField.SetValueWithoutNotify(FrequencyOptions[1].Text);

            _startDateRow.style.display = DisplayStyle.Flex;
            _startDateReadonlyRow.style.display = DisplayStyle.None;
            _startDateField.SetValueWithoutNotify(DateFormat.ForInput(DateTime.Now));

            _dayOfMonthRow.style.display = DisplayStyle.Flex;
            _dayOfMonthField.SetValueWithoutNotify(string.Empty);

            _categoryRow.style.display = DisplayStyle.Flex;
            RebuildCategoryChoices();

            _counterpartyRow.style.display = DisplayStyle.Flex;
            _counterpartyField.SetValueWithoutNotify(string.Empty);

            _deleteButton.style.display = DisplayStyle.None;
            _toggleActiveButton.style.display = DisplayStyle.None;
            _submitButton.text = "Créer";
            HideError();
            HideSkipWarning();

            ApplyTypeVisibility(TypeOptions[0].Text);
            _formCard.style.display = DisplayStyle.Flex;
        }

        // Public only so UISmokeTest.cs can open the edit form directly — its real trigger is the
        // MultiColumnListView's own selectionChanged event, which (like Button.clicked) never
        // fires on a VisualTreeAsset instantiated without a panel. Same "public purely for
        // testability" reasoning as AccountsController.OpenEditForm.
        public void OpenEditForm(RecurringOperationRowViewModel row)
        {
            _editingOperationId = row.Id;
            _editingIsActive = row.IsActive;

            _formTitle.text = row.Name;

            _nameRow.style.display = DisplayStyle.None;

            // Type, accounts, frequency, day-of-month, category and counterparty are all
            // editable in edit mode too — only name and start date stay fixed after creation
            // (start date is immutable in Domain by design; renaming has no reported need yet).
            // See RecurringOperationService.UpdateOperation, ADR-140 (extends ADR-137's
            // account-only correction to the operation's other fields).
            _typeRow.style.display = DisplayStyle.Flex;
            _typeField.SetValueWithoutNotify(row.TypeText);

            SetChoices(_sourceAccountField, _creatableAccounts);
            SelectAccount(_sourceAccountField, row.SourceAccountId);
            SetChoices(_destinationAccountField, _creatableAccounts);
            SelectAccount(_destinationAccountField, row.DestinationAccountId);
            ApplyTypeVisibility(row.TypeText);

            _amountField.SetValueWithoutNotify(RawAmountText(row.AmountText));

            _frequencyRow.style.display = DisplayStyle.Flex;
            _frequencyField.SetValueWithoutNotify(row.FrequencyText);

            _startDateRow.style.display = DisplayStyle.None;
            _startDateReadonlyRow.style.display = DisplayStyle.Flex;
            _startDateReadonlyLabel.text = row.StartDateText;

            _dayOfMonthRow.style.display = DisplayStyle.Flex;
            _dayOfMonthField.SetValueWithoutNotify(row.ExpectedDayOfMonth?.ToString() ?? string.Empty);

            _categoryRow.style.display = DisplayStyle.Flex;
            RebuildCategoryChoices();
            SelectCategory(row.CategoryId);

            _counterpartyRow.style.display = DisplayStyle.Flex;
            _counterpartyField.SetValueWithoutNotify(row.CounterpartyText == "—" ? string.Empty : row.CounterpartyText);

            _deleteButton.style.display = DisplayStyle.Flex;
            _toggleActiveButton.style.display = DisplayStyle.Flex;
            _toggleActiveButton.text = row.IsActive ? "Suspendre" : "Reprendre";
            _submitButton.text = "Enregistrer";
            HideError();
            HideSkipWarning();

            _formCard.style.display = DisplayStyle.Flex;
        }

        private void CloseForm()
        {
            _formCard.style.display = DisplayStyle.None;
            _editingOperationId = null;
        }

        private void ApplyTypeVisibility(string typeText)
        {
            var typeIndex = TypeOptions.ToList().FindIndex(o => o.Text == typeText);
            var type = typeIndex < 0 ? TypeOptions[0].Type : TypeOptions[typeIndex].Type;
            var (needsSource, needsDestination) = AccountRequirementsFor(type);
            var isTransfer = type is RecurringOperationType.SavingsTransfer or RecurringOperationType.InternalTransfer;

            _sourceAccountRow.style.display = needsSource ? DisplayStyle.Flex : DisplayStyle.None;
            _sourceAccountLabel.text = isTransfer ? "Compte source" : "Compte";

            _destinationAccountRow.style.display = needsDestination ? DisplayStyle.Flex : DisplayStyle.None;
            _destinationAccountLabel.text = isTransfer ? "Compte destination" : "Compte";
        }

        /// <summary>Which account fields a type actually needs — the one piece of validation
        /// logic shared by creation, edit-mode account correction, and this form's own field
        /// visibility, kept in one place so the three can't quietly drift apart.</summary>
        private static (bool NeedsSource, bool NeedsDestination) AccountRequirementsFor(RecurringOperationType type) => (
            type is RecurringOperationType.Expense or RecurringOperationType.SavingsTransfer or RecurringOperationType.InternalTransfer,
            type is RecurringOperationType.Income or RecurringOperationType.SavingsTransfer or RecurringOperationType.InternalTransfer);

        /// <summary>Public only so UISmokeTest.cs can drive it directly, same reasoning as
        /// <see cref="OpenEditForm"/> — its own trigger (a Button.clicked) can't be invoked from
        /// another assembly without a real panel either.</summary>
        public void SubmitForm()
        {
            if (_editingOperationId is int id)
            {
                SubmitEdit(id);
            }
            else
            {
                SubmitCreate();
            }
        }

        private void SubmitEdit(int id)
        {
            if (!TryGatherOperationFields(
                    out var type, out var magnitude, out var frequency, out var dayOfMonth,
                    out var sourceAccountId, out var destinationAccountId, out var categoryId, out var counterpartyId))
            {
                return;
            }

            try
            {
                _operations.UpdateOperation(
                    id, type, sourceAccountId, destinationAccountId, magnitude, frequency, dayOfMonth,
                    categoryId, counterpartyId);
            }
            catch (ArgumentException ex)
            {
                ShowError(ex.Message);
                return;
            }

            _operations.GenerateUpcomingOccurrences(DateTime.Now, _settings.Get().ForecastHorizonDays);

            CloseForm();
            Refresh();
        }

        private void SubmitCreate()
        {
            var name = _nameField.value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                ShowError("Le nom est requis.");
                return;
            }

            if (!DateFormat.TryParseInput(_startDateField.value, out var startDate))
            {
                ShowError("La date de début doit être au format jj/mm/aaaa.");
                return;
            }

            if (!TryGatherOperationFields(
                    out var type, out var magnitude, out var frequency, out var dayOfMonth,
                    out var sourceAccountId, out var destinationAccountId, out var categoryId, out var counterpartyId))
            {
                return;
            }

            try
            {
                _operations.Create(
                    name, type, magnitude, frequency, startDate, sourceAccountId, destinationAccountId,
                    categoryId, counterpartyId, expectedDayOfMonth: dayOfMonth);
            }
            catch (ArgumentException ex)
            {
                ShowError(ex.Message);
                return;
            }

            _operations.GenerateUpcomingOccurrences(DateTime.Now, _settings.Get().ForecastHorizonDays);

            CloseForm();
            Refresh();
        }

        /// <summary>Reads and validates every field shared by creation and editing (everything
        /// except name/start date, which only creation gathers itself). Extracted once both forms
        /// needed the exact same type/frequency/day-of-month/accounts/category/counterparty
        /// parsing — see ADR-140. Returns false (and shows the relevant error) on the first
        /// invalid field.</summary>
        private bool TryGatherOperationFields(
            out RecurringOperationType type,
            out long expectedAmountMinor,
            out RecurringFrequency frequency,
            out int? dayOfMonth,
            out int? sourceAccountId,
            out int? destinationAccountId,
            out int? categoryId,
            out int? counterpartyId)
        {
            type = default;
            expectedAmountMinor = 0;
            frequency = default;
            dayOfMonth = null;
            sourceAccountId = null;
            destinationAccountId = null;
            categoryId = null;
            counterpartyId = null;

            if (!MoneyFormat.TryParseEurosToMinor(_amountField.value, out var magnitude) || magnitude <= 0)
            {
                ShowError("Le montant doit être un nombre positif, ex. 45,90.");
                return false;
            }

            expectedAmountMinor = magnitude;

            var typeIndex = TypeOptions.ToList().FindIndex(o => o.Text == _typeField.value);
            type = TypeOptions[typeIndex < 0 ? 0 : typeIndex].Type;

            var frequencyIndex = FrequencyOptions.ToList().FindIndex(o => o.Text == _frequencyField.value);
            frequency = FrequencyOptions[frequencyIndex < 0 ? 0 : frequencyIndex].Frequency;

            if (!string.IsNullOrWhiteSpace(_dayOfMonthField.value))
            {
                if (!int.TryParse(_dayOfMonthField.value, out var parsedDay) || parsedDay is < 1 or > 31)
                {
                    ShowError("Le jour du mois doit être un nombre entre 1 et 31.");
                    return false;
                }

                dayOfMonth = parsedDay;
            }

            var (needsSource, needsDestination) = AccountRequirementsFor(type);

            if (needsSource)
            {
                if (_sourceAccountField.index < 0 || _sourceAccountField.index >= _creatableAccounts.Count)
                {
                    ShowError("Choisissez un compte.");
                    return false;
                }

                sourceAccountId = _creatableAccounts[_sourceAccountField.index].Id;
            }

            if (needsDestination)
            {
                if (_destinationAccountField.index < 0 || _destinationAccountField.index >= _creatableAccounts.Count)
                {
                    ShowError("Choisissez un compte.");
                    return false;
                }

                destinationAccountId = _creatableAccounts[_destinationAccountField.index].Id;
            }

            if (sourceAccountId is not null && destinationAccountId is not null && sourceAccountId == destinationAccountId)
            {
                ShowError("Le compte destination doit être différent du compte source.");
                return false;
            }

            categoryId = _categoryField.index <= 0 ? (int?)null : _categoryOptions[_categoryField.index - 1].Id;

            var counterpartyName = _counterpartyField.value?.Trim();
            counterpartyId = string.IsNullOrEmpty(counterpartyName)
                ? (int?)null
                : _counterparties.FindOrCreateByName(counterpartyName).Id;

            return true;
        }

        private void DeleteOperation()
        {
            if (_editingOperationId is not int id)
            {
                return;
            }

            try
            {
                _operations.Delete(id);
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
                return;
            }

            CloseForm();
            Refresh();
        }

        /// <summary>Live preview, recomputed on every relevant field change while creating — warns
        /// before submission if the chosen start date/day-of-month combination would silently skip
        /// the first cycle (the exact trap that produced a real, hard-to-diagnose missing occurrence
        /// — see the "Real user data revealed two bugs" note in project memory). Never blocks
        /// submission: the combination is still valid, just possibly not what the user meant.</summary>
        private void UpdateSkipWarning()
        {
            if (_editingOperationId is not null)
            {
                return;
            }

            if (!DateFormat.TryParseInput(_startDateField.value, out var startDate))
            {
                HideSkipWarning();
                return;
            }

            int? dayOfMonth = null;
            if (!string.IsNullOrWhiteSpace(_dayOfMonthField.value))
            {
                if (!int.TryParse(_dayOfMonthField.value, out var parsedDay) || parsedDay is < 1 or > 31)
                {
                    HideSkipWarning();
                    return;
                }

                dayOfMonth = parsedDay;
            }

            var frequencyIndex = FrequencyOptions.ToList().FindIndex(o => o.Text == _frequencyField.value);
            var frequency = FrequencyOptions[frequencyIndex < 0 ? 0 : frequencyIndex].Frequency;

            var firstDate = _operations.PreviewFirstOccurrenceDate(frequency, startDate, dayOfMonth);
            if (firstDate is { } date && (date.Year != startDate.Year || date.Month != startDate.Month))
            {
                _skipWarningLabel.text =
                    $"Avec ces réglages, le premier versement prévu sera le {DateFormat.Short(date)}, pas avant — le jour du mois choisi est déjà passé par rapport à la date de début. Ajuste l'un des deux si ce n'est pas voulu.";
                _skipWarningLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                HideSkipWarning();
            }
        }

        private void HideSkipWarning()
        {
            _skipWarningLabel.text = string.Empty;
            _skipWarningLabel.style.display = DisplayStyle.None;
        }

        private void ToggleActive()
        {
            if (_editingOperationId is not int id)
            {
                return;
            }

            if (_editingIsActive)
            {
                _operations.Suspend(id);
            }
            else
            {
                _operations.Resume(id);
                _operations.GenerateUpcomingOccurrences(DateTime.Now, _settings.Get().ForecastHorizonDays);
            }

            CloseForm();
            Refresh();
        }

        private static void SetChoices(DropdownField field, IReadOnlyList<DropdownOption> options)
        {
            var choices = options.Select(o => o.Name).ToList();
            field.choices = choices;
            field.SetValueWithoutNotify(choices.Count > 0 ? choices[0] : string.Empty);
        }

        /// <summary>Overrides SetChoices's own "select the first choice" default with the
        /// account this row actually has — used only in edit mode. Silently leaves the field on
        /// its first-choice fallback if the account isn't found (e.g. archived since creation,
        /// so it no longer appears among _creatableAccounts) rather than guessing.</summary>
        private void SelectAccount(DropdownField field, int? accountId)
        {
            if (accountId is not int id)
            {
                return;
            }

            var index = _creatableAccounts.ToList().FindIndex(o => o.Id == id);
            if (index >= 0)
            {
                field.SetValueWithoutNotify(field.choices[index]);
            }
        }

        private void RebuildCategoryChoices()
        {
            var choices = new List<string> { "Aucune" };
            choices.AddRange(_categoryOptions.Select(c => c.Name));
            _categoryField.choices = choices;
            _categoryField.SetValueWithoutNotify(choices[0]);
        }

        /// <summary>Overrides RebuildCategoryChoices's own "Aucune" default with the category this
        /// row actually has — used only in edit mode, same role as <see cref="SelectAccount"/>.
        /// The +1 offset accounts for "Aucune" always being choice 0.</summary>
        private void SelectCategory(int? categoryId)
        {
            if (categoryId is not int id)
            {
                return;
            }

            var index = _categoryOptions.ToList().FindIndex(o => o.Id == id);
            if (index >= 0)
            {
                _categoryField.SetValueWithoutNotify(_categoryField.choices[index + 1]);
            }
        }

        /// <summary>Strips the currency symbol/sign back out of an already-formatted amount so it
        /// can seed the (editable) amount field — the row view model only carries display text,
        /// never the raw minor-unit value. See MoneyFormat.Format/TryParseEurosToMinor.</summary>
        private static string RawAmountText(string amountText) =>
            amountText.Replace("−", string.Empty).Replace("+", string.Empty).Replace("€", string.Empty).Trim();

        private void ShowError(string message)
        {
            _errorLabel.text = message;
            _errorLabel.style.display = DisplayStyle.Flex;
        }

        private void HideError()
        {
            _errorLabel.text = string.Empty;
            _errorLabel.style.display = DisplayStyle.None;
        }
    }
}
