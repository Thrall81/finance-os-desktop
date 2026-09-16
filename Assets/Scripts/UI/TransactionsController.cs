using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.App;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Binds Transactions.uxml: filterable list, manual creation (expense/income/internal
    /// transfer) and — in edit mode — category, counterparty, notes and budget exclusion.
    /// Holds only the App services this screen touches, same pattern as AccountsController.
    /// See docs/07-Interface.md §3/§7/§9/§10.
    /// </summary>
    public sealed class TransactionsController
    {
        private static readonly string[] TypeOptions = { "Dépense", "Revenu", "Virement interne" };

        private readonly AccountService _accounts;
        private readonly CategoryService _categories;
        private readonly CounterpartyService _counterparties;
        private readonly TransactionService _transactions;
        private readonly InternalTransferService _internalTransfers;
        private readonly TransferDetectionService _transferDetection;
        private readonly AppSettingsService _settings;

        private readonly DropdownField _filterAccountField;
        private readonly DropdownField _filterCategoryField;
        private readonly TextField _filterDateFromField;
        private readonly TextField _filterDateToField;
        private readonly TextField _filterAmountMinField;
        private readonly TextField _filterAmountMaxField;
        private readonly TextField _filterTextField;
        private readonly Button _filterResetButton;
        private readonly Button _newTransactionButton;
        private readonly Button _newTransactionButtonList;

        private readonly Label _transferSuggestionsCountLabel;
        private readonly Label _transferSuggestionsEmptyLabel;
        private readonly VisualElement _transferSuggestionsList;

        private readonly VisualElement _formCard;
        private readonly Label _formTitle;
        private readonly VisualElement _typeRow;
        private readonly DropdownField _typeField;
        private readonly VisualElement _accountRow;
        private readonly Label _accountLabel;
        private readonly DropdownField _accountField;
        private readonly VisualElement _accountReadonlyRow;
        private readonly Label _accountReadonlyLabel;
        private readonly VisualElement _destinationAccountRow;
        private readonly DropdownField _destinationAccountField;
        private readonly VisualElement _dateRow;
        private readonly Button _dateField;
        private DateTime _dateValue;
        private readonly VisualElement _dateReadonlyRow;
        private readonly Label _dateReadonlyLabel;
        private readonly VisualElement _amountRow;
        private readonly TextField _amountField;
        private readonly VisualElement _amountReadonlyRow;
        private readonly Label _amountReadonlyLabel;
        private readonly VisualElement _labelRow;
        private readonly TextField _labelField;
        private readonly VisualElement _labelReadonlyRow;
        private readonly Label _labelReadonlyLabel;
        private readonly VisualElement _categoryRow;
        private readonly DropdownField _categoryField;
        private readonly VisualElement _counterpartyRow;
        private readonly TextField _counterpartyField;
        private readonly TextField _notesField;
        private readonly VisualElement _excludedRow;
        private readonly Toggle _excludedToggle;
        private readonly Label _errorLabel;
        private readonly Button _deleteButton;
        private readonly Button _cancelButton;
        private readonly Button _submitButton;

        private readonly Label _emptyLabel;
        private readonly MultiColumnListView _listView;

        private IReadOnlyList<DropdownOption> _filterAccountOptions = Array.Empty<DropdownOption>();
        private IReadOnlyList<DropdownOption> _creatableAccounts = Array.Empty<DropdownOption>();
        private IReadOnlyList<DropdownOption> _categoryOptions = Array.Empty<DropdownOption>();
        private List<TransactionRowViewModel> _rows = new();
        private IReadOnlyDictionary<int, string> _accountNames = new Dictionary<int, string>();

        private int? _filterAccountId;
        private int? _filterCategoryId;
        private DateTime? _filterDateFrom;
        private DateTime? _filterDateTo;
        private long? _filterAmountMinMinor;
        private long? _filterAmountMaxMinor;
        private string? _filterText;

        private int? _editingTransactionId;
        private bool _categoryManuallySet;

        public TransactionsController(
            VisualElement root,
            AccountService accounts,
            CategoryService categories,
            CounterpartyService counterparties,
            TransactionService transactions,
            InternalTransferService internalTransfers,
            TransferDetectionService transferDetection,
            AppSettingsService settings,
            StyleSheet? appUiThemeStyleSheet = null,
            bool isDarkTheme = false)
        {
            _accounts = accounts;
            _categories = categories;
            _counterparties = counterparties;
            _transactions = transactions;
            _internalTransfers = internalTransfers;
            _transferDetection = transferDetection;
            _settings = settings;

            _filterAccountField = root.Q<DropdownField>("filter-account");
            _filterCategoryField = root.Q<DropdownField>("filter-category");
            _filterDateFromField = root.Q<TextField>("filter-date-from");
            _filterDateToField = root.Q<TextField>("filter-date-to");
            _filterAmountMinField = root.Q<TextField>("filter-amount-min");
            NumericInputFilter.RestrictToDecimal(_filterAmountMinField);
            _filterAmountMaxField = root.Q<TextField>("filter-amount-max");
            NumericInputFilter.RestrictToDecimal(_filterAmountMaxField);
            _filterTextField = root.Q<TextField>("filter-text");
            _filterResetButton = root.Q<Button>("filters-reset-button");
            _newTransactionButton = root.Q<Button>("new-transaction-button");
            _newTransactionButtonList = root.Q<Button>("new-transaction-button-list");

            _transferSuggestionsCountLabel = root.Q<Label>("transfer-suggestions-count");
            _transferSuggestionsEmptyLabel = root.Q<Label>("transfer-suggestions-empty");
            _transferSuggestionsList = root.Q<VisualElement>("transfer-suggestions-list");

            _formCard = root.Q<VisualElement>("transaction-form-card");
            _formTitle = root.Q<Label>("form-title");
            _typeRow = root.Q<VisualElement>("form-type-row");
            _typeField = root.Q<DropdownField>("form-type");
            _accountRow = root.Q<VisualElement>("form-account-row");
            _accountLabel = root.Q<Label>("form-account-label");
            _accountField = root.Q<DropdownField>("form-account");
            _accountReadonlyRow = root.Q<VisualElement>("form-account-readonly-row");
            _accountReadonlyLabel = root.Q<Label>("form-account-readonly");
            _destinationAccountRow = root.Q<VisualElement>("form-destination-account-row");
            _destinationAccountField = root.Q<DropdownField>("form-destination-account");
            _dateRow = root.Q<VisualElement>("form-date-row");
            _dateField = root.Q<Button>("form-date");
            AppDatePickerField.Attach(
                _dateField, () => _dateValue, selected => _dateValue = selected,
                isDarkTheme, appUiThemeStyleSheet);
            _dateReadonlyRow = root.Q<VisualElement>("form-date-readonly-row");
            _dateReadonlyLabel = root.Q<Label>("form-date-readonly");
            _amountRow = root.Q<VisualElement>("form-amount-row");
            _amountField = root.Q<TextField>("form-amount");
            NumericInputFilter.RestrictToDecimal(_amountField);
            _amountReadonlyRow = root.Q<VisualElement>("form-amount-readonly-row");
            _amountReadonlyLabel = root.Q<Label>("form-amount-readonly");
            _labelRow = root.Q<VisualElement>("form-label-row");
            _labelField = root.Q<TextField>("form-label-field");
            _labelReadonlyRow = root.Q<VisualElement>("form-label-readonly-row");
            _labelReadonlyLabel = root.Q<Label>("form-label-readonly");
            _categoryRow = root.Q<VisualElement>("form-category-row");
            _categoryField = root.Q<DropdownField>("form-category");
            _counterpartyRow = root.Q<VisualElement>("form-counterparty-row");
            _counterpartyField = root.Q<TextField>("form-counterparty");
            _notesField = root.Q<TextField>("form-notes");
            _excludedRow = root.Q<VisualElement>("form-excluded-row");
            _excludedToggle = root.Q<Toggle>("form-excluded-toggle");
            _errorLabel = root.Q<Label>("form-error");
            _deleteButton = root.Q<Button>("form-delete-button");
            _cancelButton = root.Q<Button>("form-cancel-button");
            _submitButton = root.Q<Button>("form-submit-button");

            _emptyLabel = root.Q<Label>("transactions-empty");
            _listView = root.Q<MultiColumnListView>("transactions-list-view");
            TableHeaderTheme.Wire(_listView, isDarkTheme);

            _typeField.choices = TypeOptions.ToList();

            SetupColumns();

            _filterAccountField.RegisterValueChangedCallback(_ => OnFilterChanged());
            _filterCategoryField.RegisterValueChangedCallback(_ => OnFilterChanged());
            _filterDateFromField.RegisterValueChangedCallback(_ => OnFilterChanged());
            _filterDateToField.RegisterValueChangedCallback(_ => OnFilterChanged());
            _filterAmountMinField.RegisterValueChangedCallback(_ => OnFilterChanged());
            _filterAmountMaxField.RegisterValueChangedCallback(_ => OnFilterChanged());
            _filterTextField.RegisterValueChangedCallback(_ => OnFilterChanged());
            _filterResetButton.clicked += ResetFilters;
            _newTransactionButton.clicked += OpenCreateForm;
            // Same action duplicated next to the list itself — see AccountsController's
            // identical wiring and ADR-142 for why.
            _newTransactionButtonList.clicked += OpenCreateForm;
            _cancelButton.clicked += CloseForm;
            _submitButton.clicked += SubmitForm;
            _deleteButton.clicked += DeleteTransaction;
            _typeField.RegisterValueChangedCallback(evt => ApplyTypeVisibility(evt.newValue));
            _labelField.RegisterValueChangedCallback(OnLabelChanged);
            _categoryField.RegisterValueChangedCallback(_ =>
            {
                _categoryManuallySet = true;
                _categoryField.RemoveFromClassList("field-suggested");
            });
            _listView.selectionChanged += _ => OnRowSelected();

            Refresh();
        }

        private void SetupColumns()
        {
            _listView.columns.Add(BuildColumn("date", "Date", r => r.DateText, width: 130, minWidth: 110, mono: true));
            _listView.columns.Add(BuildColumn("account", "Compte", r => r.AccountName, width: 130, minWidth: 100));
            _listView.columns.Add(BuildColumn("label", "Libellé", r => r.Label, width: 220, minWidth: 120, stretchable: true));
            _listView.columns.Add(BuildColumn("category", "Catégorie", r => r.CategoryText, width: 130, minWidth: 100));
            _listView.columns.Add(BuildColumn("amount", "Montant", r => r.AmountText, width: 110, minWidth: 90, alignRight: true));
            _listView.selectionType = SelectionType.Single;
            _listView.fixedItemHeight = 28;
        }

        private Column BuildColumn(
            string name, string title, Func<TransactionRowViewModel, string> textSelector,
            float width, float minWidth, bool alignRight = false, bool stretchable = false, bool mono = false)
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

                    if (mono)
                    {
                        label.AddToClassList("cell-mono");
                    }

                    return label;
                },
                bindCell = (element, index) => ((Label)element).text = textSelector(_rows[index]),
            };
        }

        public void Refresh()
        {
            var filter = new TransactionFilter(
                _filterAccountId, _filterCategoryId, _filterDateFrom, _filterDateTo,
                _filterAmountMinMinor, _filterAmountMaxMinor, _filterText);
            var viewModel = TransactionsViewModelBuilder.Build(_accounts, _categories, _counterparties, _transactions, filter);

            _creatableAccounts = viewModel.CreatableAccounts;
            _categoryOptions = viewModel.Categories;
            _rows = viewModel.Transactions.ToList();
            _accountNames = viewModel.AccountFilterOptions.ToDictionary(o => o.Id, o => o.Name);

            RebuildFilterChoices(viewModel.AccountFilterOptions);
            RebuildFilterCategoryChoices(viewModel.Categories);
            _newTransactionButton.SetEnabled(_creatableAccounts.Count > 0);
            _newTransactionButtonList.SetEnabled(_creatableAccounts.Count > 0);

            var hasRows = _rows.Count > 0;
            _emptyLabel.style.display = hasRows ? DisplayStyle.None : DisplayStyle.Flex;
            _listView.style.display = hasRows ? DisplayStyle.Flex : DisplayStyle.None;
            _listView.itemsSource = _rows;
            // Rebuild, not just RefreshItems — this list's display is toggled between None and
            // Flex, and a bare item refresh left stale geometry from before the toggle in place,
            // rendering the table over its own card title. See ADR-116.
            _listView.Rebuild();

            RenderTransferSuggestions();
        }

        // Recomputed on every Refresh() (account-scale data, cheap) rather than cached, so
        // confirming/rejecting one suggestion — or creating/editing/deleting a transaction that
        // changes what pairs up — is reflected immediately without a separate invalidation path.
        private void RenderTransferSuggestions()
        {
            var candidates = _transferDetection.DetectCandidates();
            _transferSuggestionsCountLabel.text = candidates.Count.ToString();
            _transferSuggestionsEmptyLabel.style.display = candidates.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            _transferSuggestionsList.Clear();
            foreach (var candidate in candidates)
            {
                _transferSuggestionsList.Add(BuildTransferSuggestionRow(candidate));
            }
        }

        private VisualElement BuildTransferSuggestionRow(TransferCandidate candidate)
        {
            var element = new VisualElement();
            element.AddToClassList("verification-row");

            var textColumn = new VisualElement { style = { flexGrow = 1 } };
            var labelElement = new Label($"{ResolveAccountName(candidate.Outgoing.AccountId)} → {ResolveAccountName(candidate.Incoming.AccountId)}");
            labelElement.AddToClassList("verification-row-label");
            var dateElement = new Label(candidate.Outgoing.OperationDate == candidate.Incoming.OperationDate
                ? DateFormat.Short(candidate.Outgoing.OperationDate)
                : $"{DateFormat.Short(candidate.Outgoing.OperationDate)} → {DateFormat.Short(candidate.Incoming.OperationDate)}");
            dateElement.AddToClassList("verification-row-date");
            textColumn.Add(labelElement);
            textColumn.Add(dateElement);

            var amountElement = new Label(MoneyFormat.Format(Math.Abs(candidate.Outgoing.AmountMinor), candidate.Outgoing.Currency));
            amountElement.AddToClassList("verification-row-amount");

            var actions = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var confirmButton = new Button(() => ConfirmTransferSuggestion(candidate)) { text = "Confirmer" };
            confirmButton.AddToClassList("secondary-button");
            var rejectButton = new Button(() => RejectTransferSuggestion(candidate)) { text = "Ignorer" };
            rejectButton.AddToClassList("secondary-button");
            actions.Add(confirmButton);
            actions.Add(rejectButton);

            element.Add(textColumn);
            element.Add(amountElement);
            element.Add(actions);
            return element;
        }

        private void ConfirmTransferSuggestion(TransferCandidate candidate)
        {
            _transferDetection.Confirm(candidate.Outgoing.Id, candidate.Incoming.Id);
            Refresh();
        }

        private void RejectTransferSuggestion(TransferCandidate candidate)
        {
            _transferDetection.Reject(candidate.Outgoing.Id, candidate.Incoming.Id);
            Refresh();
        }

        private string ResolveAccountName(int accountId) =>
            _accountNames.TryGetValue(accountId, out var name) ? name : "—";

        private void RebuildFilterChoices(IReadOnlyList<DropdownOption> options)
        {
            _filterAccountOptions = options;
            var choices = new List<string> { "Tous les comptes" };
            choices.AddRange(options.Select(o => o.Name));
            _filterAccountField.choices = choices;

            var selectedIndex = _filterAccountId is int currentId
                ? options.ToList().FindIndex(o => o.Id == currentId) + 1
                : 0;
            _filterAccountField.SetValueWithoutNotify(choices[Math.Max(selectedIndex, 0)]);
        }

        private void RebuildFilterCategoryChoices(IReadOnlyList<DropdownOption> options)
        {
            var choices = new List<string> { "Toutes les catégories" };
            choices.AddRange(options.Select(o => o.Name));
            _filterCategoryField.choices = choices;

            var selectedIndex = _filterCategoryId is int currentId
                ? options.ToList().FindIndex(o => o.Id == currentId) + 1
                : 0;
            _filterCategoryField.SetValueWithoutNotify(choices[Math.Max(selectedIndex, 0)]);
        }

        // Every filter field re-reads its own value on each change rather than tracking deltas,
        // and an unparseable date/amount is treated as "no constraint on that field" rather than
        // blocking the whole filter — this is a live, incremental search box, not a submitted
        // form, so it must never show a validation error while the user is mid-keystroke.
        // Public only so UISmokeTest.cs can trigger it directly: an instantiated-but-unattached
        // VisualTreeAsset has no panel, so the ChangeEvent a real .value assignment sends never
        // dispatches in batchmode — same "public purely for testability" reasoning as
        // OnboardingController (ADR-130), applied here to a field callback instead of a click.
        public void OnFilterChanged()
        {
            var accountIndex = _filterAccountField.index;
            _filterAccountId = accountIndex <= 0 ? null : _filterAccountOptions[accountIndex - 1].Id;

            var categoryIndex = _filterCategoryField.index;
            _filterCategoryId = categoryIndex <= 0 ? null : _categoryOptions[categoryIndex - 1].Id;

            _filterDateFrom = DateFormat.TryParseInput(_filterDateFromField.value, out var dateFrom) ? dateFrom : null;
            _filterDateTo = DateFormat.TryParseInput(_filterDateToField.value, out var dateTo) ? dateTo : null;

            _filterAmountMinMinor = MoneyFormat.TryParseEurosToMinor(_filterAmountMinField.value, out var amountMin) ? amountMin : null;
            _filterAmountMaxMinor = MoneyFormat.TryParseEurosToMinor(_filterAmountMaxField.value, out var amountMax) ? amountMax : null;

            _filterText = string.IsNullOrWhiteSpace(_filterTextField.value) ? null : _filterTextField.value.Trim();

            Refresh();
        }

        private void ResetFilters()
        {
            _filterAccountId = null;
            _filterCategoryId = null;
            _filterDateFrom = null;
            _filterDateTo = null;
            _filterAmountMinMinor = null;
            _filterAmountMaxMinor = null;
            _filterText = null;

            _filterDateFromField.SetValueWithoutNotify(string.Empty);
            _filterDateToField.SetValueWithoutNotify(string.Empty);
            _filterAmountMinField.SetValueWithoutNotify(string.Empty);
            _filterAmountMaxField.SetValueWithoutNotify(string.Empty);
            _filterTextField.SetValueWithoutNotify(string.Empty);

            Refresh();
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

        /// <summary>Public only so UISmokeTest.cs can reopen the create form directly to check the
        /// default-account preselection after Paramètres' default account changes — its real
        /// trigger (Button.clicked) can't be invoked from another assembly either, same reasoning
        /// as OpenEditForm/SubmitForm.</summary>
        public void OpenCreateForm()
        {
            _editingTransactionId = null;
            _categoryManuallySet = false;

            _formTitle.text = "Nouvelle transaction";

            _typeRow.style.display = DisplayStyle.Flex;
            _typeField.SetValueWithoutNotify(TypeOptions[0]);

            _accountRow.style.display = DisplayStyle.Flex;
            _accountReadonlyRow.style.display = DisplayStyle.None;
            var defaultAccountId = _settings.Get().DefaultCurrentAccountId;
            SetChoices(_accountField, _creatableAccounts, defaultAccountId);
            SetChoices(_destinationAccountField, _creatableAccounts, defaultAccountId);

            _dateRow.style.display = DisplayStyle.Flex;
            _dateReadonlyRow.style.display = DisplayStyle.None;
            _dateValue = DateTime.Now;
            _dateField.text = DateFormat.ForInput(_dateValue);

            _amountRow.style.display = DisplayStyle.Flex;
            _amountReadonlyRow.style.display = DisplayStyle.None;
            _amountField.SetValueWithoutNotify(string.Empty);

            _labelRow.style.display = DisplayStyle.Flex;
            _labelReadonlyRow.style.display = DisplayStyle.None;
            _labelField.SetValueWithoutNotify(string.Empty);

            RebuildCategoryChoices();
            _categoryField.RemoveFromClassList("field-suggested");

            _counterpartyField.SetValueWithoutNotify(string.Empty);
            _notesField.SetValueWithoutNotify(string.Empty);
            _excludedRow.style.display = DisplayStyle.None;

            _deleteButton.style.display = DisplayStyle.None;
            _submitButton.text = "Créer";
            HideError();

            ApplyTypeVisibility(TypeOptions[0]);
            _formCard.style.display = DisplayStyle.Flex;
        }

        private void OpenEditForm(TransactionRowViewModel row)
        {
            _editingTransactionId = row.Id;
            _categoryManuallySet = true;

            _formTitle.text = row.Label;

            _typeRow.style.display = DisplayStyle.None;

            _accountRow.style.display = DisplayStyle.None;
            _accountReadonlyRow.style.display = DisplayStyle.Flex;
            _accountReadonlyLabel.text = row.AccountName;

            _destinationAccountRow.style.display = DisplayStyle.None;

            _dateRow.style.display = DisplayStyle.None;
            _dateReadonlyRow.style.display = DisplayStyle.Flex;
            _dateReadonlyLabel.text = row.DateText;

            _amountRow.style.display = DisplayStyle.None;
            _amountReadonlyRow.style.display = DisplayStyle.Flex;
            _amountReadonlyLabel.text = row.AmountText;

            _labelRow.style.display = DisplayStyle.None;
            _labelReadonlyRow.style.display = DisplayStyle.Flex;
            _labelReadonlyLabel.text = row.Label;

            if (row.IsInternalTransfer)
            {
                _categoryRow.style.display = DisplayStyle.None;
                _counterpartyRow.style.display = DisplayStyle.None;
            }
            else
            {
                _categoryRow.style.display = DisplayStyle.Flex;
                RebuildCategoryChoices();
                var categoryIndex = row.CategoryId is int categoryId
                    ? _categoryOptions.ToList().FindIndex(o => o.Id == categoryId) + 1
                    : 0;
                _categoryField.SetValueWithoutNotify(_categoryField.choices[Math.Max(categoryIndex, 0)]);
                _categoryField.RemoveFromClassList("field-suggested");

                _counterpartyRow.style.display = DisplayStyle.Flex;
                _counterpartyField.SetValueWithoutNotify(row.CounterpartyText == "—" ? string.Empty : row.CounterpartyText);
            }

            _notesField.SetValueWithoutNotify(row.Notes);
            _excludedRow.style.display = DisplayStyle.Flex;
            _excludedToggle.SetValueWithoutNotify(row.IsExcludedFromBudget);

            _deleteButton.style.display = DisplayStyle.Flex;
            _submitButton.text = "Enregistrer";
            HideError();

            _formCard.style.display = DisplayStyle.Flex;
        }

        private void CloseForm()
        {
            _formCard.style.display = DisplayStyle.None;
            _editingTransactionId = null;
        }

        private void ApplyTypeVisibility(string typeText)
        {
            var isTransfer = typeText == TypeOptions[2];
            _accountLabel.text = isTransfer ? "Compte source" : "Compte";
            _destinationAccountRow.style.display = isTransfer ? DisplayStyle.Flex : DisplayStyle.None;
            _categoryRow.style.display = isTransfer ? DisplayStyle.None : DisplayStyle.Flex;
            _counterpartyRow.style.display = isTransfer ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void SubmitForm()
        {
            if (_editingTransactionId is int id)
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
            var categoryId = ResolveSelectedCategoryId();
            _transactions.AssignCategory(id, categoryId);

            var counterpartyId = ResolveOrCreateCounterpartyId(_counterpartyField.value);
            _transactions.AssignCounterparty(id, counterpartyId);

            var notes = string.IsNullOrWhiteSpace(_notesField.value) ? null : _notesField.value.Trim();
            _transactions.UpdateNotes(id, notes);

            _transactions.SetExcludedFromBudget(id, _excludedToggle.value);

            CloseForm();
            Refresh();
        }

        private void SubmitCreate()
        {
            if (!MoneyFormat.TryParseEurosToMinor(_amountField.value, out var magnitude) || magnitude <= 0)
            {
                ShowError("Le montant doit être un nombre positif, ex. 45,90.");
                return;
            }

            var date = _dateValue;

            var label = _labelField.value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(label))
            {
                ShowError("Le libellé est requis.");
                return;
            }

            if (_accountField.index < 0 || _accountField.index >= _creatableAccounts.Count)
            {
                ShowError("Choisissez un compte.");
                return;
            }

            var accountId = _creatableAccounts[_accountField.index].Id;
            var isTransfer = _typeField.value == TypeOptions[2];

            try
            {
                if (isTransfer)
                {
                    if (_destinationAccountField.index < 0 || _destinationAccountField.index >= _creatableAccounts.Count)
                    {
                        ShowError("Choisissez un compte destination.");
                        return;
                    }

                    var destinationAccountId = _creatableAccounts[_destinationAccountField.index].Id;
                    if (destinationAccountId == accountId)
                    {
                        ShowError("Le compte destination doit être différent du compte source.");
                        return;
                    }

                    _internalTransfers.CreateTransfer(accountId, destinationAccountId, magnitude, "EUR", date, label);
                }
                else
                {
                    var amount = _typeField.value == TypeOptions[0] ? -magnitude : magnitude;
                    var categoryId = ResolveSelectedCategoryId();
                    var counterpartyId = ResolveOrCreateCounterpartyId(_counterpartyField.value);
                    var notes = string.IsNullOrWhiteSpace(_notesField.value) ? null : _notesField.value.Trim();

                    _transactions.CreateManual(accountId, amount, "EUR", date, label, categoryId, counterpartyId, notes);
                }
            }
            catch (ArgumentException ex)
            {
                ShowError(ex.Message);
                return;
            }

            CloseForm();
            Refresh();
        }

        private void DeleteTransaction()
        {
            if (_editingTransactionId is not int id)
            {
                return;
            }

            _transactions.DeleteManual(id);
            CloseForm();
            Refresh();
        }

        private void OnLabelChanged(ChangeEvent<string> evt)
        {
            if (_categoryManuallySet || string.IsNullOrWhiteSpace(evt.newValue))
            {
                return;
            }

            var suggestedCategoryId = _transactions.SuggestCategoryForLabel(evt.newValue);
            if (suggestedCategoryId is not int categoryId)
            {
                return;
            }

            var index = _categoryOptions.ToList().FindIndex(o => o.Id == categoryId);
            if (index < 0)
            {
                return;
            }

            _categoryField.SetValueWithoutNotify(_categoryField.choices[index + 1]);
            _categoryField.AddToClassList("field-suggested");
        }

        private int? ResolveSelectedCategoryId()
        {
            var index = _categoryField.index;
            return index <= 0 ? null : _categoryOptions[index - 1].Id;
        }

        private int? ResolveOrCreateCounterpartyId(string? name)
        {
            var trimmed = name?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : _counterparties.FindOrCreateByName(trimmed).Id;
        }

        /// <summary>Populates a dropdown's choices and preselects <paramref name="preferredAccountId"/>
        /// (the user's default account, Paramètres) when it's among the options — falls back to the
        /// first choice otherwise, same as before this had a preference at all.</summary>
        private static void SetChoices(DropdownField field, IReadOnlyList<DropdownOption> options, int? preferredAccountId = null)
        {
            var choices = options.Select(o => o.Name).ToList();
            field.choices = choices;

            var preferredIndex = preferredAccountId is int id ? options.ToList().FindIndex(o => o.Id == id) : -1;
            field.SetValueWithoutNotify(preferredIndex >= 0 ? choices[preferredIndex] : choices.Count > 0 ? choices[0] : string.Empty);
        }

        private void RebuildCategoryChoices()
        {
            var choices = new List<string> { "Aucune" };
            choices.AddRange(_categoryOptions.Select(c => c.Name));
            _categoryField.choices = choices;
            _categoryField.SetValueWithoutNotify(choices[0]);
        }

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
