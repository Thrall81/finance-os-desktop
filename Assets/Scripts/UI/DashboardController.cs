using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Binds a <see cref="DashboardViewModel"/> onto the Dashboard.uxml tree — no formatting
    /// logic here, everything already arrives as display-ready text from
    /// <see cref="DashboardViewModelBuilder"/>. See docs/07-Interface.md §5.
    /// </summary>
    public sealed class DashboardController
    {
        private readonly Label _accountNameLabel;
        private readonly Label _availableValueLabel;
        private readonly Label _endOfMonthValueLabel;
        private readonly Label _lowestValueLabel;
        private readonly Label _lowestDateLabel;
        private readonly Label _verificationCountLabel;
        private readonly Label _verificationEmptyLabel;
        private readonly VisualElement _verificationList;

        public DashboardController(VisualElement root)
        {
            _accountNameLabel = root.Q<Label>("account-name-label");
            _availableValueLabel = root.Q<Label>("kpi-available-value");
            _endOfMonthValueLabel = root.Q<Label>("kpi-endofmonth-value");
            _lowestValueLabel = root.Q<Label>("kpi-lowest-value");
            _lowestDateLabel = root.Q<Label>("kpi-lowest-date");
            _verificationCountLabel = root.Q<Label>("verification-count");
            _verificationEmptyLabel = root.Q<Label>("verification-empty");
            _verificationList = root.Q<VisualElement>("verification-list");
        }

        public void Render(DashboardViewModel viewModel)
        {
            _accountNameLabel.text = viewModel.AccountName;
            _availableValueLabel.text = viewModel.AvailableBalanceText;
            _endOfMonthValueLabel.text = viewModel.EndOfMonthBalanceText;
            _lowestValueLabel.text = viewModel.LowestBalanceText;
            _lowestDateLabel.text = viewModel.LowestBalanceDateText;

            _verificationCountLabel.text = viewModel.VerificationQueue.Count.ToString();
            _verificationEmptyLabel.style.display = viewModel.VerificationQueue.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _verificationList.Clear();
            foreach (var item in viewModel.VerificationQueue)
            {
                _verificationList.Add(BuildVerificationRow(item));
            }
        }

        private static VisualElement BuildVerificationRow(DashboardVerificationItem item)
        {
            var row = new VisualElement();
            row.AddToClassList("verification-row");

            var textColumn = new VisualElement { style = { flexGrow = 1 } };
            var labelElement = new Label(item.Label);
            labelElement.AddToClassList("verification-row-label");
            var dateElement = new Label(item.DateText);
            dateElement.AddToClassList("verification-row-date");
            textColumn.Add(labelElement);
            textColumn.Add(dateElement);

            var amountElement = new Label(item.AmountText);
            amountElement.AddToClassList("verification-row-amount");

            row.Add(textColumn);
            row.Add(amountElement);
            return row;
        }
    }
}
