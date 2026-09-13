using UnityEngine;
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
        private readonly Label _remainingValueLabel;
        private readonly Label _verificationCountLabel;
        private readonly Label _verificationEmptyLabel;
        private readonly VisualElement _verificationList;
        private readonly LineChartElement _cashFlowChart;
        private readonly Label _donutEmptyLabel;
        private readonly VisualElement _donutRow;
        private readonly ExpenseDonutElement _donutChart;
        private readonly VisualElement _donutLegend;
        private readonly Label _upcomingEmptyLabel;
        private readonly VisualElement _upcomingList;
        private readonly Label _budgetSummaryEmptyLabel;
        private readonly VisualElement _budgetSummaryList;

        public DashboardController(VisualElement root)
        {
            _accountNameLabel = root.Q<Label>("account-name-label");
            _availableValueLabel = root.Q<Label>("kpi-available-value");
            _endOfMonthValueLabel = root.Q<Label>("kpi-endofmonth-value");
            _lowestValueLabel = root.Q<Label>("kpi-lowest-value");
            _lowestDateLabel = root.Q<Label>("kpi-lowest-date");
            _remainingValueLabel = root.Q<Label>("kpi-remaining-value");
            _verificationCountLabel = root.Q<Label>("verification-count");
            _verificationEmptyLabel = root.Q<Label>("verification-empty");
            _verificationList = root.Q<VisualElement>("verification-list");

            _cashFlowChart = new LineChartElement();
            _cashFlowChart.style.flexGrow = 1;
            root.Q<VisualElement>("cashflow-chart-container").Add(_cashFlowChart);

            _donutEmptyLabel = root.Q<Label>("donut-empty");
            _donutRow = root.Q<VisualElement>("donut-row");
            _donutChart = new ExpenseDonutElement();
            _donutChart.style.flexGrow = 1;
            root.Q<VisualElement>("donut-chart-container").Add(_donutChart);
            _donutLegend = root.Q<VisualElement>("donut-legend");

            _upcomingEmptyLabel = root.Q<Label>("upcoming-empty");
            _upcomingList = root.Q<VisualElement>("upcoming-list");

            _budgetSummaryEmptyLabel = root.Q<Label>("budget-summary-empty");
            _budgetSummaryList = root.Q<VisualElement>("budget-summary-list");
        }

        public void Render(DashboardViewModel viewModel)
        {
            _accountNameLabel.text = viewModel.AccountName;
            _availableValueLabel.text = viewModel.AvailableBalanceText;
            _endOfMonthValueLabel.text = viewModel.EndOfMonthBalanceText;
            _lowestValueLabel.text = viewModel.LowestBalanceText;
            _lowestDateLabel.text = viewModel.LowestBalanceDateText;
            _remainingValueLabel.text = viewModel.RemainingToLiveText;

            _cashFlowChart.Points = viewModel.ChartSeries;

            var hasExpenses = viewModel.ExpenseBreakdown.Count > 0;
            _donutEmptyLabel.style.display = hasExpenses ? DisplayStyle.None : DisplayStyle.Flex;
            _donutRow.style.display = hasExpenses ? DisplayStyle.Flex : DisplayStyle.None;
            _donutChart.Slices = viewModel.ExpenseBreakdown;

            _donutLegend.Clear();
            for (var i = 0; i < viewModel.ExpenseBreakdown.Count; i++)
            {
                _donutLegend.Add(BuildDonutLegendRow(viewModel.ExpenseBreakdown[i], ExpenseDonutElement.Palette[i % ExpenseDonutElement.Palette.Length]));
            }

            _verificationCountLabel.text = viewModel.VerificationQueue.Count.ToString();
            _verificationEmptyLabel.style.display = viewModel.VerificationQueue.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _verificationList.Clear();
            foreach (var item in viewModel.VerificationQueue)
            {
                _verificationList.Add(BuildVerificationRow(item));
            }

            _upcomingEmptyLabel.style.display = viewModel.UpcomingOperations.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _upcomingList.Clear();
            foreach (var item in viewModel.UpcomingOperations)
            {
                _upcomingList.Add(BuildUpcomingRow(item));
            }

            _budgetSummaryEmptyLabel.style.display = viewModel.BudgetSummary.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _budgetSummaryList.Clear();
            foreach (var row in viewModel.BudgetSummary)
            {
                _budgetSummaryList.Add(BuildBudgetSummaryRow(row));
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

        private static VisualElement BuildUpcomingRow(DashboardUpcomingItem item)
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

            var rightColumn = new VisualElement();
            rightColumn.AddToClassList("upcoming-row-right");
            var amountElement = new Label(item.AmountText);
            amountElement.AddToClassList("verification-row-amount");
            var certaintyElement = new Label(item.CertaintyText);
            certaintyElement.AddToClassList(item.CertaintyText == "Attendue" ? "badge" : "badge-muted");
            certaintyElement.AddToClassList("upcoming-row-badge");
            rightColumn.Add(amountElement);
            rightColumn.Add(certaintyElement);

            row.Add(textColumn);
            row.Add(rightColumn);
            return row;
        }

        private static VisualElement BuildBudgetSummaryRow(DashboardBudgetRowViewModel row)
        {
            var container = new VisualElement();
            container.AddToClassList("budget-summary-row");

            var top = new VisualElement();
            top.AddToClassList("budget-summary-top");
            var nameLabel = new Label(row.CategoryName);
            nameLabel.AddToClassList("budget-summary-name");
            var figuresLabel = new Label($"{row.ActualText} / {row.PlannedText}");
            figuresLabel.AddToClassList("budget-summary-figures");
            top.Add(nameLabel);
            top.Add(figuresLabel);

            var track = new VisualElement();
            track.AddToClassList("budget-summary-bar-track");
            var fill = new VisualElement();
            fill.AddToClassList("budget-summary-bar-fill");
            fill.EnableInClassList("budget-summary-bar-fill-over", row.IsOverBudget);
            fill.style.width = new Length(row.SpentRatio * 100f, LengthUnit.Percent);
            track.Add(fill);

            var noteLabel = new Label(row.NoteText);
            noteLabel.AddToClassList("budget-summary-note");
            noteLabel.EnableInClassList("budget-summary-note-over", row.IsOverBudget);

            container.Add(top);
            container.Add(track);
            container.Add(noteLabel);
            return container;
        }

        private static VisualElement BuildDonutLegendRow(ExpenseCategorySliceViewModel slice, Color color)
        {
            var row = new VisualElement();
            row.AddToClassList("donut-legend-row");

            var swatch = new VisualElement();
            swatch.AddToClassList("donut-legend-swatch");
            swatch.style.backgroundColor = color;

            var nameLabel = new Label(slice.CategoryName);
            nameLabel.AddToClassList("donut-legend-name");

            var valueLabel = new Label($"{slice.AmountText} · {slice.PercentText}");
            valueLabel.AddToClassList("donut-legend-value");

            row.Add(swatch);
            row.Add(nameLabel);
            row.Add(valueLabel);
            return row;
        }
    }
}
