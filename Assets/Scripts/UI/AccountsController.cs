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

        private readonly Button _newAccountButton;
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

        private int? _editingAccountId;
        private bool _liquidityManuallySet;

        public AccountsController(VisualElement root, AccountService accounts)
        {
            _accounts = accounts;

            _newAccountButton = root.Q<Button>("new-account-button");
            _formCard = root.Q<VisualElement>("account-form-card");
            _formTitle = root.Q<Label>("form-title");
            _nameField = root.Q<TextField>("form-name");
            _typeField = root.Q<DropdownField>("form-type");
            _balanceField = root.Q<TextField>("form-balance");
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

            _typeField.choices = TypeOptions.Select(o => o.Text).ToList();
            _liquidityField.choices = LiquidityOptions.Select(o => o.Text).ToList();

            _newAccountButton.clicked += OpenCreateForm;
            _cancelButton.clicked += CloseForm;
            _submitButton.clicked += SubmitForm;
            _archiveButton.clicked += ToggleArchive;
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
            _submitButton.text = "Créer";
            HideError();

            _formCard.style.display = DisplayStyle.Flex;
        }

        private void OpenEditForm(AccountRowViewModel row)
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
