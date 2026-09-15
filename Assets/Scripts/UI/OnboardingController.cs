using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Binds Onboarding.uxml: the 3-to-4-step first-launch flow (docs/01-Perimetre.md §2.1,
    /// docs/07-Interface.md §4) — bienvenue, premier compte (required), charges et revenus
    /// principaux (facultatif), récapitulatif. Every add persists immediately through
    /// AccountService/RecurringOperationService, exactly like every other screen — there is
    /// nothing to lose by leaving mid-flow, since nothing is buffered locally waiting for a final
    /// "save". <see cref="OnFinished"/> is called only when the user reaches "Terminer"; the
    /// gating decision (never show this again once an account exists) lives in AppBootstrap, not
    /// here. See docs/09-Decisions_techniques.md ADR-130.
    /// </summary>
    public sealed class OnboardingController
    {
        private static readonly (AccountType Type, string Text)[] AccountTypeOptions =
        {
            (AccountType.Current, AccountsViewModelBuilder.TypeText(AccountType.Current)),
            (AccountType.Savings, AccountsViewModelBuilder.TypeText(AccountType.Savings)),
            (AccountType.Cash, AccountsViewModelBuilder.TypeText(AccountType.Cash)),
            (AccountType.Investment, AccountsViewModelBuilder.TypeText(AccountType.Investment)),
            (AccountType.Debt, AccountsViewModelBuilder.TypeText(AccountType.Debt)),
        };

        // Minimal on purpose (docs/07-Interface.md §4's "formulaire minimal") — épargne/virement
        // interne are real recurring-operation types but out of scope for "salaire, loyer, une ou
        // deux factures", and asking for a day-of-month here would reopen the exact start-date/
        // day-of-month mismatch trap ADR-120 fixed elsewhere; every operation created here starts
        // today, so its day-of-month always matches its start date by construction.
        private static readonly (RecurringOperationType Type, string Text)[] OperationTypeOptions =
        {
            (RecurringOperationType.Expense, "Dépense"),
            (RecurringOperationType.Income, "Revenu"),
        };

        private readonly AccountService _accounts;
        private readonly RecurringOperationService _operations;
        private readonly Action _onFinished;

        private readonly Label _stepIndicatorLabel;

        private readonly VisualElement _stepWelcome;
        private readonly Button _welcomeNextButton;

        private readonly VisualElement _stepAccount;
        private readonly TextField _accountNameField;
        private readonly DropdownField _accountTypeField;
        private readonly TextField _accountBalanceField;
        private readonly Label _accountErrorLabel;
        private readonly Button _accountAddButton;
        private readonly VisualElement _accountList;
        private readonly Button _accountBackButton;
        private readonly Button _accountNextButton;
        private readonly Label _accountNextErrorLabel;

        private readonly VisualElement _stepRecurring;
        private readonly TextField _operationNameField;
        private readonly DropdownField _operationTypeField;
        private readonly TextField _operationAmountField;
        private readonly DropdownField _operationAccountField;
        private readonly Label _operationErrorLabel;
        private readonly Button _operationAddButton;
        private readonly VisualElement _operationList;
        private readonly Button _recurringBackButton;
        private readonly Button _recurringNextButton;

        private readonly VisualElement _stepRecap;
        private readonly VisualElement _recapAccountList;
        private readonly Label _recapOperationsEmptyLabel;
        private readonly VisualElement _recapOperationList;
        private readonly Button _recapBackButton;
        private readonly Button _recapFinishButton;

        private IReadOnlyList<DropdownOption> _accountOptions = Array.Empty<DropdownOption>();

        public OnboardingController(VisualElement root, AccountService accounts, RecurringOperationService operations, Action onFinished)
        {
            _accounts = accounts;
            _operations = operations;
            _onFinished = onFinished;

            _stepIndicatorLabel = root.Q<Label>("step-indicator");

            _stepWelcome = root.Q<VisualElement>("step-welcome");
            _welcomeNextButton = root.Q<Button>("welcome-next-button");

            _stepAccount = root.Q<VisualElement>("step-account");
            _accountNameField = root.Q<TextField>("account-name-field");
            _accountTypeField = root.Q<DropdownField>("account-type-field");
            _accountBalanceField = root.Q<TextField>("account-balance-field");
            NumericInputFilter.RestrictToDecimal(_accountBalanceField, allowNegative: true);
            _accountErrorLabel = root.Q<Label>("account-error");
            _accountAddButton = root.Q<Button>("account-add-button");
            _accountList = root.Q<VisualElement>("account-list");
            _accountBackButton = root.Q<Button>("account-back-button");
            _accountNextButton = root.Q<Button>("account-next-button");
            _accountNextErrorLabel = root.Q<Label>("account-next-error");

            _stepRecurring = root.Q<VisualElement>("step-recurring");
            _operationNameField = root.Q<TextField>("operation-name-field");
            _operationTypeField = root.Q<DropdownField>("operation-type-field");
            _operationAmountField = root.Q<TextField>("operation-amount-field");
            NumericInputFilter.RestrictToDecimal(_operationAmountField);
            _operationAccountField = root.Q<DropdownField>("operation-account-field");
            _operationErrorLabel = root.Q<Label>("operation-error");
            _operationAddButton = root.Q<Button>("operation-add-button");
            _operationList = root.Q<VisualElement>("operation-list");
            _recurringBackButton = root.Q<Button>("recurring-back-button");
            _recurringNextButton = root.Q<Button>("recurring-next-button");

            _stepRecap = root.Q<VisualElement>("step-recap");
            _recapAccountList = root.Q<VisualElement>("recap-account-list");
            _recapOperationsEmptyLabel = root.Q<Label>("recap-operations-empty");
            _recapOperationList = root.Q<VisualElement>("recap-operation-list");
            _recapBackButton = root.Q<Button>("recap-back-button");
            _recapFinishButton = root.Q<Button>("recap-finish-button");

            _accountTypeField.choices = AccountTypeOptions.Select(o => o.Text).ToList();
            _accountTypeField.SetValueWithoutNotify(AccountTypeOptions[0].Text);
            _operationTypeField.choices = OperationTypeOptions.Select(o => o.Text).ToList();
            _operationTypeField.SetValueWithoutNotify(OperationTypeOptions[0].Text);

            _welcomeNextButton.clicked += () => GoToStep(1);
            _accountAddButton.clicked += AddAccount;
            _accountBackButton.clicked += () => GoToStep(0);
            _accountNextButton.clicked += TryAdvanceFromAccountStep;
            _operationAddButton.clicked += AddOperation;
            _recurringBackButton.clicked += () => GoToStep(1);
            _recurringNextButton.clicked += () => GoToStep(3);
            _recapBackButton.clicked += () => GoToStep(2);
            _recapFinishButton.clicked += Finish;

            GoToStep(0);
        }

        /// <summary>Switches the visible step (0 Bienvenue, 1 Premier compte, 2 Charges et
        /// revenus, 3 Récapitulatif) and re-renders. Public, along with <see cref="AddAccount"/>,
        /// <see cref="AddOperation"/>, <see cref="TryAdvanceFromAccountStep"/> and
        /// <see cref="Finish"/>, purely so UISmokeTest can drive the whole flow directly —
        /// <c>Button.clicked</c> cannot be invoked from outside the class, so without a public
        /// surface only the constructor's initial state would be testable at all. Same reasoning
        /// as ADR-120/124/129's pure-function-made-public pattern, applied here to a stateful
        /// wizard instead of a pure calculation.</summary>
        public void GoToStep(int step)
        {
            _stepWelcome.style.display = step == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _stepAccount.style.display = step == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            _stepRecurring.style.display = step == 2 ? DisplayStyle.Flex : DisplayStyle.None;
            _stepRecap.style.display = step == 3 ? DisplayStyle.Flex : DisplayStyle.None;
            _stepIndicatorLabel.text = $"Étape {step + 1} sur 4";

            Refresh();
        }

        private void Refresh()
        {
            var viewModel = OnboardingViewModelBuilder.Build(_accounts, _operations);
            _accountOptions = viewModel.AccountOptions;

            _accountList.Clear();
            foreach (var row in viewModel.Accounts)
            {
                _accountList.Add(BuildAccountRow(row));
            }

            _operationList.Clear();
            foreach (var row in viewModel.Operations)
            {
                _operationList.Add(BuildOperationRow(row));
            }

            var accountChoices = _accountOptions.Select(o => o.Name).ToList();
            _operationAccountField.choices = accountChoices;
            if (accountChoices.Count > 0)
            {
                _operationAccountField.SetValueWithoutNotify(accountChoices[0]);
            }

            _recapAccountList.Clear();
            foreach (var row in viewModel.Accounts)
            {
                _recapAccountList.Add(BuildAccountRow(row));
            }

            var hasOperations = viewModel.Operations.Count > 0;
            _recapOperationsEmptyLabel.style.display = hasOperations ? DisplayStyle.None : DisplayStyle.Flex;
            _recapOperationList.Clear();
            foreach (var row in viewModel.Operations)
            {
                _recapOperationList.Add(BuildOperationRow(row));
            }

            _recurringNextButton.text = hasOperations ? "Suivant" : "Passer";
        }

        public void AddAccount()
        {
            var name = _accountNameField.value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                ShowAccountError("Le nom du compte est requis.");
                return;
            }

            if (!MoneyFormat.TryParseEurosToMinor(_accountBalanceField.value, out var balanceMinor))
            {
                ShowAccountError("Le solde actuel doit être un montant valide, ex. 1234,56.");
                return;
            }

            var typeIndex = AccountTypeOptions.ToList().FindIndex(o => o.Text == _accountTypeField.value);
            var type = AccountTypeOptions[typeIndex < 0 ? 0 : typeIndex].Type;

            // Liquidity policy deliberately omitted — Account's own constructor already applies a
            // sensible default per type (docs/01-Perimetre.md §2.2), same as every other screen
            // that creates an account without asking for it up front.
            _accounts.CreateAccount(name, type, "EUR", balanceMinor);

            _accountNameField.SetValueWithoutNotify(string.Empty);
            _accountBalanceField.SetValueWithoutNotify(string.Empty);
            HideAccountError();
            Refresh();
        }

        public void TryAdvanceFromAccountStep()
        {
            if (_accountOptions.Count == 0)
            {
                _accountNextErrorLabel.text = "Créez au moins un compte pour continuer.";
                _accountNextErrorLabel.style.display = DisplayStyle.Flex;
                return;
            }

            _accountNextErrorLabel.style.display = DisplayStyle.None;
            GoToStep(2);
        }

        public void AddOperation()
        {
            var name = _operationNameField.value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                ShowOperationError("Le nom est requis.");
                return;
            }

            if (!MoneyFormat.TryParseEurosToMinor(_operationAmountField.value, out var magnitude) || magnitude <= 0)
            {
                ShowOperationError("Le montant doit être un nombre positif, ex. 1200,00.");
                return;
            }

            if (_operationAccountField.index < 0 || _operationAccountField.index >= _accountOptions.Count)
            {
                ShowOperationError("Choisissez un compte.");
                return;
            }

            var typeIndex = OperationTypeOptions.ToList().FindIndex(o => o.Text == _operationTypeField.value);
            var type = OperationTypeOptions[typeIndex < 0 ? 0 : typeIndex].Type;
            var accountId = _accountOptions[_operationAccountField.index].Id;
            var today = DateTime.Now;

            _operations.Create(
                name, type, magnitude, RecurringFrequency.Monthly, today,
                sourceAccountId: type == RecurringOperationType.Expense ? accountId : null,
                destinationAccountId: type == RecurringOperationType.Income ? accountId : null);

            _operationNameField.SetValueWithoutNotify(string.Empty);
            _operationAmountField.SetValueWithoutNotify(string.Empty);
            HideOperationError();
            Refresh();
        }

        public void Finish() => _onFinished();

        private static VisualElement BuildAccountRow(OnboardingAccountRowViewModel row) => BuildRow(row.Name, row.TypeText, row.BalanceText);

        private static VisualElement BuildOperationRow(OnboardingOperationRowViewModel row) => BuildRow(row.Name, row.TypeText, row.AmountText);

        private static VisualElement BuildRow(string name, string metaText, string valueText)
        {
            var element = new VisualElement();
            element.AddToClassList("verification-row");

            var textColumn = new VisualElement { style = { flexGrow = 1 } };
            var nameLabel = new Label(name);
            nameLabel.AddToClassList("verification-row-label");
            var metaLabel = new Label(metaText);
            metaLabel.AddToClassList("verification-row-date");
            textColumn.Add(nameLabel);
            textColumn.Add(metaLabel);

            var valueLabel = new Label(valueText);
            valueLabel.AddToClassList("verification-row-amount");

            element.Add(textColumn);
            element.Add(valueLabel);
            return element;
        }

        private void ShowAccountError(string message)
        {
            _accountErrorLabel.text = message;
            _accountErrorLabel.style.display = DisplayStyle.Flex;
        }

        private void HideAccountError()
        {
            _accountErrorLabel.text = string.Empty;
            _accountErrorLabel.style.display = DisplayStyle.None;
        }

        private void ShowOperationError(string message)
        {
            _operationErrorLabel.text = message;
            _operationErrorLabel.style.display = DisplayStyle.Flex;
        }

        private void HideOperationError()
        {
            _operationErrorLabel.text = string.Empty;
            _operationErrorLabel.style.display = DisplayStyle.None;
        }
    }
}
