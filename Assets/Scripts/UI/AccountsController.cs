using System;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Binds Accounts.uxml: list, create, rename, liquidity policy and archive/restore. Holds
    /// only AccountService, not the full AppContainer, since that is all this screen ever touches.
    /// Self-contained: rebuilds and re-renders itself after every mutation rather than relying on
    /// an outside refresh call. See docs/07-Interface.md §3/§9.
    /// </summary>
    public sealed class AccountsController
    {
        private static readonly (AccountType Type, string Text)[] TypeOptions =
        {
            (AccountType.Current, AccountsViewModelBuilder.TypeText(AccountType.Current)),
            (AccountType.Savings, AccountsViewModelBuilder.TypeText(AccountType.Savings)),
            (AccountType.Cash, AccountsViewModelBuilder.TypeText(AccountType.Cash)),
            (AccountType.Investment, AccountsViewModelBuilder.TypeText(AccountType.Investment)),
            (AccountType.Debt, AccountsViewModelBuilder.TypeText(AccountType.Debt)),
        };

        private static readonly (LiquidityPolicy Policy, string Text)[] LiquidityOptions =
        {
            (LiquidityPolicy.Immediate, AccountsViewModelBuilder.LiquidityPolicyText(LiquidityPolicy.Immediate)),
            (LiquidityPolicy.Reserve, AccountsViewModelBuilder.LiquidityPolicyText(LiquidityPolicy.Reserve)),
            (LiquidityPolicy.Excluded, AccountsViewModelBuilder.LiquidityPolicyText(LiquidityPolicy.Excluded)),
        };

        private readonly AccountService _accounts;
        private readonly AppSettingsService _settings;

        private readonly Button _newAccountButton;
        private readonly Button _newAccountButtonList;
        private readonly VisualElement _formCard;
        private readonly Label _formTitle;
        private readonly TextField _nameField;
        private readonly DropdownField _typeField;
        private readonly TextField _balanceField;
        private readonly VisualElement _balanceRow;
        private readonly TextField _institutionField;
        private readonly VisualElement _institutionRow;
        private readonly DropdownField _liquidityField;
        private readonly Label _errorLabel;
        private readonly Button _cancelButton;
        private readonly Button _archiveButton;
        private readonly Button _submitButton;
        private readonly Label _emptyLabel;
        private readonly VisualElement _list;

        private readonly VisualElement _balanceHistorySection;
        private readonly Button _balanceHistoryDateField;
        private DateTime _balanceHistoryDateValue;
        private readonly TextField _balanceHistoryAmountField;
        private readonly Button _balanceHistorySubmitButton;
        private readonly Label _balanceHistoryErrorLabel;
        private readonly Label _balanceHistoryEmptyLabel;
        private readonly VisualElement _balanceHistoryList;

        private int? _editingAccountId;
        private bool _liquidityManuallySet;

        public AccountsController(
            VisualElement root, AccountService accounts, AppSettingsService settings,
            StyleSheet? appUiThemeStyleSheet = null)
        {
            _accounts = accounts;
            _settings = settings;

            _newAccountButton = root.Q<Button>("new-account-button");
            _newAccountButtonList = root.Q<Button>("new-account-button-list");
            _formCard = root.Q<VisualElement>("account-form-card");
            _formTitle = root.Q<Label>("form-title");
            _nameField = root.Q<TextField>("form-name");
            _typeField = root.Q<DropdownField>("form-type");
            _balanceField = root.Q<TextField>("form-balance");
            NumericInputFilter.RestrictToDecimal(_balanceField, allowNegative: true);
            _balanceRow = root.Q<VisualElement>("form-balance-row");
            _institutionField = root.Q<TextField>("form-institution");
            _institutionRow = root.Q<VisualElement>("form-institution-row");
            _liquidityField = root.Q<DropdownField>("form-liquidity");
            _errorLabel = root.Q<Label>("form-error");
            _cancelButton = root.Q<Button>("form-cancel-button");
            _archiveButton = root.Q<Button>("form-archive-button");
            _submitButton = root.Q<Button>("form-submit-button");
            _emptyLabel = root.Q<Label>("accounts-empty");
            _list = root.Q<VisualElement>("accounts-list");

            _balanceHistorySection = root.Q<VisualElement>("balance-history-section");
            // Set explicitly rather than relying on the UXML inline style alone — see ADR-117:
            // a bare "style=display:none;" does not reliably populate .style.display outside a
            // live panel.
            _balanceHistorySection.style.display = DisplayStyle.None;
            _balanceHistoryDateField = root.Q<Button>("balance-history-date");
            AppDatePickerField.Attach(
                _balanceHistoryDateField, () => _balanceHistoryDateValue,
                selected => _balanceHistoryDateValue = selected,
                settings.Get().Theme == AppTheme.Dark, appUiThemeStyleSheet);
            _balanceHistoryAmountField = root.Q<TextField>("balance-history-amount");
            NumericInputFilter.RestrictToDecimal(_balanceHistoryAmountField, allowNegative: true);
            _balanceHistorySubmitButton = root.Q<Button>("balance-history-submit-button");
            _balanceHistoryErrorLabel = root.Q<Label>("balance-history-error");
            _balanceHistoryEmptyLabel = root.Q<Label>("balance-history-empty");
            _balanceHistoryList = root.Q<VisualElement>("balance-history-list");

            _typeField.choices = TypeOptions.Select(o => o.Text).ToList();
            _liquidityField.choices = LiquidityOptions.Select(o => o.Text).ToList();

            _newAccountButton.clicked += OpenCreateForm;
            // Same action duplicated next to the list itself, not just at the top of the page —
            // real user feedback: testers saw the top button but reached for one near the list by
            // reflex instead. See docs/09-Decisions_techniques.md ADR-142.
            _newAccountButtonList.clicked += OpenCreateForm;
            _cancelButton.clicked += CloseForm;
            _submitButton.clicked += SubmitForm;
            _archiveButton.clicked += ToggleArchive;
            _balanceHistorySubmitButton.clicked += SubmitBalanceHistory;
            _typeField.RegisterValueChangedCallback(OnTypeChanged);
            _liquidityField.RegisterValueChangedCallback(_ => _liquidityManuallySet = true);

            Refresh();
        }

        public void Refresh()
        {
            var viewModel = AccountsViewModelBuilder.Build(_accounts);

            _emptyLabel.style.display = viewModel.Accounts.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            _list.Clear();
            foreach (var row in viewModel.Accounts)
            {
                _list.Add(BuildRow(row));
            }
        }

        private VisualElement BuildRow(AccountRowViewModel row)
        {
            var element = new VisualElement();
            element.AddToClassList("account-row");
            if (row.IsArchived)
            {
                element.AddToClassList("account-row-archived");
            }

            var textColumn = new VisualElement { style = { flexGrow = 1 } };
            var nameLabel = new Label(row.Name);
            nameLabel.AddToClassList("account-row-name");
            var metaLabel = new Label($"{row.TypeText} · {row.LiquidityPolicyText}{(row.IsArchived ? " · Archivé" : string.Empty)}");
            metaLabel.AddToClassList("account-row-meta");
            textColumn.Add(nameLabel);
            textColumn.Add(metaLabel);

            var balanceLabel = new Label(row.BalanceText);
            balanceLabel.AddToClassList("account-row-balance");

            element.Add(textColumn);
            element.Add(balanceLabel);
            element.RegisterCallback<ClickEvent>(_ => OpenEditForm(row));

            return element;
        }

        private void OpenCreateForm()
        {
            _editingAccountId = null;
            _liquidityManuallySet = false;

            _formTitle.text = "Nouveau compte";
            _nameField.SetValueWithoutNotify(string.Empty);
            _typeField.SetValueWithoutNotify(TypeOptions[0].Text);
            _balanceField.SetValueWithoutNotify("0");
            _institutionField.SetValueWithoutNotify(string.Empty);
            _liquidityField.SetValueWithoutNotify(
                LiquidityOptions.First(o => o.Policy == Account.DefaultLiquidityPolicyFor(TypeOptions[0].Type)).Text);

            _typeField.SetEnabled(true);
            _balanceRow.style.display = DisplayStyle.Flex;
            _institutionRow.style.display = DisplayStyle.Flex;
            _archiveButton.style.display = DisplayStyle.None;
            _balanceHistorySection.style.display = DisplayStyle.None;
            _submitButton.text = "Créer";
            HideError();

            _formCard.style.display = DisplayStyle.Flex;
        }

        // Public only so UISmokeTest.cs can open the edit form directly: it's normally reached by
        // a row ClickEvent (element.RegisterCallback<ClickEvent>), which — like Button.clicked —
        // never dispatches on a VisualTreeAsset instantiated without a panel (see ADR-113). Same
        // "public purely for testability" reasoning as TransactionsController.OnFilterChanged.
        public void OpenEditForm(AccountRowViewModel row)
        {
            _editingAccountId = row.Id;
            _liquidityManuallySet = true;

            _formTitle.text = row.Name;
            _nameField.SetValueWithoutNotify(row.Name);
            _typeField.SetValueWithoutNotify(row.TypeText);
            _liquidityField.SetValueWithoutNotify(row.LiquidityPolicyText);

            _typeField.SetEnabled(false);
            _balanceRow.style.display = DisplayStyle.None;
            _institutionRow.style.display = DisplayStyle.None;
            _archiveButton.style.display = DisplayStyle.Flex;
            _archiveButton.text = row.IsArchived ? "Restaurer" : "Archiver";
            _submitButton.text = "Enregistrer";
            HideError();

            _balanceHistorySection.style.display = DisplayStyle.Flex;
            _balanceHistoryDateValue = DateTime.Now;
            _balanceHistoryDateField.text = DateFormat.ForInput(_balanceHistoryDateValue);
            _balanceHistoryAmountField.SetValueWithoutNotify(string.Empty);
            HideBalanceHistoryError();
            RenderBalanceHistory(row.Id);

            _formCard.style.display = DisplayStyle.Flex;
        }

        private void CloseForm()
        {
            _formCard.style.display = DisplayStyle.None;
            _editingAccountId = null;
        }

        private void SubmitForm()
        {
            var name = _nameField.value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                ShowError("Le nom du compte est requis.");
                return;
            }

            var liquidityIndex = LiquidityOptions.ToList().FindIndex(o => o.Text == _liquidityField.value);
            var liquidityPolicy = LiquidityOptions[liquidityIndex < 0 ? 0 : liquidityIndex].Policy;

            if (_editingAccountId is int id)
            {
                _accounts.Rename(id, name);
                _accounts.SetLiquidityPolicy(id, liquidityPolicy);
            }
            else
            {
                if (!MoneyFormat.TryParseEurosToMinor(_balanceField.value, out var initialBalanceMinor))
                {
                    ShowError("Le solde initial doit être un montant valide, ex. 1234,56.");
                    return;
                }

                var typeIndex = TypeOptions.ToList().FindIndex(o => o.Text == _typeField.value);
                var type = TypeOptions[typeIndex < 0 ? 0 : typeIndex].Type;
                var institution = string.IsNullOrWhiteSpace(_institutionField.value) ? null : _institutionField.value.Trim();

                _accounts.CreateAccount(name, type, "EUR", initialBalanceMinor, institution, liquidityPolicy);
            }

            CloseForm();
            Refresh();
        }

        private void ToggleArchive()
        {
            if (_editingAccountId is not int id)
            {
                return;
            }

            var account = _accounts.FindById(id);
            if (account is null)
            {
                return;
            }

            if (account.IsArchived)
            {
                _accounts.Restore(id);
            }
            else
            {
                _accounts.Archive(id);
            }

            CloseForm();
            Refresh();
        }

        private void OnTypeChanged(ChangeEvent<string> evt)
        {
            if (_liquidityManuallySet)
            {
                return;
            }

            var typeIndex = TypeOptions.ToList().FindIndex(o => o.Text == evt.newValue);
            if (typeIndex < 0)
            {
                return;
            }

            var defaultPolicy = Account.DefaultLiquidityPolicyFor(TypeOptions[typeIndex].Type);
            _liquidityField.SetValueWithoutNotify(LiquidityOptions.First(o => o.Policy == defaultPolicy).Text);
        }

        private void RenderBalanceHistory(int accountId)
        {
            var currency = _accounts.FindById(accountId)?.Currency ?? "EUR";
            var rows = AccountsViewModelBuilder.BuildBalanceHistory(_accounts.ListBalanceHistory(accountId), currency);

            _balanceHistoryEmptyLabel.style.display = rows.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            _balanceHistoryList.Clear();
            foreach (var row in rows)
            {
                _balanceHistoryList.Add(BuildBalanceHistoryRow(row));
            }
        }

        private static VisualElement BuildBalanceHistoryRow(BalanceHistoryRowViewModel row)
        {
            var element = new VisualElement();
            element.AddToClassList("verification-row");

            var dateLabel = new Label(row.DateText);
            dateLabel.AddToClassList("verification-row-label");

            var amountLabel = new Label(row.AmountText);
            amountLabel.AddToClassList("verification-row-amount");

            element.Add(dateLabel);
            element.Add(amountLabel);
            return element;
        }

        /// <summary>Public only so UISmokeTest.cs can drive it directly, same reasoning as
        /// <see cref="OpenEditForm"/> — its own trigger (a Button.clicked) can't be invoked from
        /// another assembly without a real panel either.</summary>
        public void SubmitBalanceHistory()
        {
            if (_editingAccountId is not int accountId)
            {
                return;
            }

            if (!MoneyFormat.TryParseEurosToMinor(_balanceHistoryAmountField.value, out var balanceMinor))
            {
                ShowBalanceHistoryError("Le solde doit être un montant valide, ex. 1234,56.");
                return;
            }

            try
            {
                _accounts.RecordOfficialBalance(accountId, balanceMinor, _balanceHistoryDateValue);
            }
            catch (ArgumentException ex)
            {
                ShowBalanceHistoryError(ex.Message);
                return;
            }

            _balanceHistoryAmountField.SetValueWithoutNotify(string.Empty);
            _balanceHistoryDateValue = DateTime.Now;
            _balanceHistoryDateField.text = DateFormat.ForInput(_balanceHistoryDateValue);
            HideBalanceHistoryError();
            RenderBalanceHistory(accountId);
            // Also refreshes the account row behind the form — its displayed balance changes
            // when this entry becomes the newest known one, see Account.RecordOfficialBalance.
            Refresh();
        }

        private void ShowBalanceHistoryError(string message)
        {
            _balanceHistoryErrorLabel.text = message;
            _balanceHistoryErrorLabel.style.display = DisplayStyle.Flex;
        }

        private void HideBalanceHistoryError()
        {
            _balanceHistoryErrorLabel.text = string.Empty;
            _balanceHistoryErrorLabel.style.display = DisplayStyle.None;
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
