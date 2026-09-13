using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.App;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Binds Forecasts.uxml: account selector, synthesis, the full "file de vérification" (with
    /// an inline confirm form for "C'est arrivé"), the read-only occurrences list, the textual
    /// timeline (accessible alternative to the deferred chart, ADR-112) and the "Et si ?"
    /// simulation. Holds the whole AppContainer, like the builder — see
    /// ForecastsViewModelBuilder's doc comment for why. See docs/07-Interface.md §3/§6/§8.
    /// </summary>
    public sealed class ForecastsController
    {
        private static readonly string[] SimulationTypeOptions = { "Dépense", "Revenu" };

        private readonly AppContainer _app;

        private readonly DropdownField _accountField;
        private readonly VisualElement _warningsList;

        private readonly Label _currentValueLabel;
        private readonly Label _projectedValueLabel;
        private readonly Label _lowestValueLabel;
        private readonly Label _lowestDateLabel;
        private readonly Label _incomeValueLabel;
        private readonly Label _expensesValueLabel;

        private readonly Label _verificationCountLabel;
        private readonly Label _verificationEmptyLabel;
        private readonly VisualElement _verificationList;

        private readonly VisualElement _confirmFormCard;
        private readonly Label _confirmFormTitle;
        private readonly TextField _confirmDateField;
        private readonly TextField _confirmAmountField;
        private readonly Label _confirmErrorLabel;
        private readonly Button _confirmCancelButton;
        private readonly Button _confirmSubmitButton;

        private readonly Label _occurrencesEmptyLabel;
        private readonly MultiColumnListView _occurrencesListView;

        private readonly LineChartElement _cashFlowChart;
        private readonly Label _timelineEmptyLabel;
        private readonly MultiColumnListView _timelineListView;

        private readonly Button _simulationToggleButton;
        private readonly VisualElement _simulationBody;
        private readonly TextField _simulationLabelField;
        private readonly DropdownField _simulationTypeField;
        private readonly TextField _simulationAmountField;
        private readonly TextField _simulationDateField;
        private readonly DropdownField _simulationCategoryField;
        private readonly Label _simulationErrorLabel;
        private readonly Button _simulationRunButton;
        private readonly VisualElement _simulationResultCard;
        private readonly Label _simulationResultProjectedLabel;
        private readonly Label _simulationResultLowestLabel;
        private readonly Label _simulationResultLowestDateLabel;
        private readonly Button _simulationResetButton;

        private IReadOnlyList<DropdownOption> _accountOptions = Array.Empty<DropdownOption>();
        private IReadOnlyList<DropdownOption> _categoryOptions = Array.Empty<DropdownOption>();
        private List<ForecastOccurrenceRowViewModel> _occurrenceRows = new();
        private List<ForecastTimelineRowViewModel> _timelineRows = new();

        private int? _selectedAccountId;
        private int? _confirmingOccurrenceId;
        private bool _confirmingIsNegative;
        private bool _simulationExpanded;

        public ForecastsController(VisualElement root, AppContainer app)
        {
            _app = app;

            _accountField = root.Q<DropdownField>("forecast-account-select");
            _warningsList = root.Q<VisualElement>("warnings-list");

            _currentValueLabel = root.Q<Label>("kpi-current-value");
            _projectedValueLabel = root.Q<Label>("kpi-projected-value");
            _lowestValueLabel = root.Q<Label>("kpi-lowest-value");
            _lowestDateLabel = root.Q<Label>("kpi-lowest-date");
            _incomeValueLabel = root.Q<Label>("kpi-income-value");
            _expensesValueLabel = root.Q<Label>("kpi-expenses-value");

            _verificationCountLabel = root.Q<Label>("verification-count");
            _verificationEmptyLabel = root.Q<Label>("verification-empty");
            _verificationList = root.Q<VisualElement>("verification-list");

            _confirmFormCard = root.Q<VisualElement>("confirm-form-card");
            _confirmFormTitle = root.Q<Label>("confirm-form-title");
            _confirmDateField = root.Q<TextField>("confirm-date");
            _confirmAmountField = root.Q<TextField>("confirm-amount");
            _confirmErrorLabel = root.Q<Label>("confirm-error");
            _confirmCancelButton = root.Q<Button>("confirm-cancel-button");
            _confirmSubmitButton = root.Q<Button>("confirm-submit-button");

            _occurrencesEmptyLabel = root.Q<Label>("occurrences-empty");
            _occurrencesListView = root.Q<MultiColumnListView>("occurrences-list-view");

            _cashFlowChart = new LineChartElement();
            // The container has a fixed height (.chart in theme.uss) but a plain VisualElement's
            // own height is content-driven, i.e. zero for one with no children — without growing
            // to fill its parent, contentRect stays empty and OnGenerateVisualContent's own
            // guard silently draws nothing. Confirmed: this was exactly why the first screenshot
            // showed a blank chart area, not a rendering-math bug.
            _cashFlowChart.style.flexGrow = 1;
            root.Q<VisualElement>("cashflow-chart-container").Add(_cashFlowChart);

            _timelineEmptyLabel = root.Q<Label>("timeline-empty");
            _timelineListView = root.Q<MultiColumnListView>("timeline-list-view");

            _simulationToggleButton = root.Q<Button>("simulation-toggle-button");
            _simulationBody = root.Q<VisualElement>("simulation-body");
            // Set explicitly rather than relying on the UXML inline style alone — confirmed
            // empirically that a bare "style=display:none" in UXML does not reliably populate
            // .style.display on a tree that was never attached to a live panel (every other
            // hidden-by-default element in this app is already set this way by its controller).
            _simulationBody.style.display = DisplayStyle.None;
            _simulationLabelField = root.Q<TextField>("simulation-label");
            _simulationTypeField = root.Q<DropdownField>("simulation-type");
            _simulationAmountField = root.Q<TextField>("simulation-amount");
            _simulationDateField = root.Q<TextField>("simulation-date");
            _simulationCategoryField = root.Q<DropdownField>("simulation-category");
            _simulationErrorLabel = root.Q<Label>("simulation-error");
            _simulationRunButton = root.Q<Button>("simulation-run-button");
            _simulationResultCard = root.Q<VisualElement>("simulation-result-card");
            _simulationResultProjectedLabel = root.Q<Label>("simulation-result-projected");
            _simulationResultLowestLabel = root.Q<Label>("simulation-result-lowest");
            _simulationResultLowestDateLabel = root.Q<Label>("simulation-result-lowest-date");
            _simulationResetButton = root.Q<Button>("simulation-reset-button");

            _simulationTypeField.choices = SimulationTypeOptions.ToList();
            _simulationTypeField.SetValueWithoutNotify(SimulationTypeOptions[0]);
            _simulationDateField.SetValueWithoutNotify(DateFormat.ForInput(DateTime.Now));

            SetupOccurrenceColumns();
            SetupTimelineColumns();

            _accountField.RegisterValueChangedCallback(_ => OnAccountChanged());
            _confirmCancelButton.clicked += CloseConfirmForm;
            _confirmSubmitButton.clicked += SubmitConfirm;
            _simulationToggleButton.clicked += ToggleSimulationBody;
            _simulationRunButton.clicked += RunSimulation;
            _simulationResetButton.clicked += () => _simulationResultCard.style.display = DisplayStyle.None;

            Refresh();
        }

        private void SetupOccurrenceColumns()
        {
            _occurrencesListView.columns.Add(BuildOccurrenceColumn("date", "Date", r => r.DateText, width: 90, minWidth: 80, mono: true));
            _occurrencesListView.columns.Add(BuildOccurrenceColumn("label", "Libellé", r => r.Label, width: 220, minWidth: 120, stretchable: true));
            _occurrencesListView.columns.Add(BuildOccurrenceColumn("amount", "Montant", r => r.AmountText, width: 110, minWidth: 90, alignRight: true));
            _occurrencesListView.columns.Add(BuildOccurrenceColumn("status", "Statut", r => r.StatusText, width: 100, minWidth: 80));
            _occurrencesListView.fixedItemHeight = 28;
        }

        private Column BuildOccurrenceColumn(
            string name, string title, Func<ForecastOccurrenceRowViewModel, string> textSelector,
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
                bindCell = (element, index) => ((Label)element).text = textSelector(_occurrenceRows[index]),
            };
        }

        private void SetupTimelineColumns()
        {
            _timelineListView.columns.Add(new Column
            {
                name = "date",
                title = "Date",
                width = 90,
                minWidth = 80,
                makeCell = () =>
                {
                    var label = new Label();
                    label.AddToClassList("cell-mono");
                    return label;
                },
                bindCell = (element, index) => ((Label)element).text = _timelineRows[index].DateText,
            });
            _timelineListView.columns.Add(new Column
            {
                name = "balance",
                title = "Solde",
                width = 110,
                minWidth = 90,
                makeCell = () =>
                {
                    var label = new Label();
                    label.AddToClassList("account-row-balance");
                    return label;
                },
                bindCell = (element, index) => ((Label)element).text = _timelineRows[index].BalanceText,
            });
            _timelineListView.columns.Add(new Column
            {
                name = "events",
                title = "Mouvements",
                width = 260,
                minWidth = 150,
                stretchable = true,
                makeCell = () => new Label(),
                bindCell = (element, index) => ((Label)element).text = _timelineRows[index].EventsText,
            });
            _timelineListView.fixedItemHeight = 28;
        }

        public void Refresh()
        {
            var viewModel = ForecastsViewModelBuilder.Build(_app, _selectedAccountId, DateTime.Now);

            _accountOptions = viewModel.Accounts;
            _categoryOptions = viewModel.Categories;
            _selectedAccountId = viewModel.SelectedAccountId;

            RebuildAccountChoices();
            RebuildCategoryChoices();

            RenderSynthesis(viewModel.Synthesis);
            RenderVerificationQueue(viewModel.VerificationQueue);
            _cashFlowChart.Points = viewModel.ChartSeries;

            _occurrenceRows = viewModel.Occurrences.ToList();
            var hasOccurrences = _occurrenceRows.Count > 0;
            _occurrencesEmptyLabel.style.display = hasOccurrences ? DisplayStyle.None : DisplayStyle.Flex;
            _occurrencesListView.style.display = hasOccurrences ? DisplayStyle.Flex : DisplayStyle.None;
            _occurrencesListView.itemsSource = _occurrenceRows;
            // Rebuild, not just RefreshItems — see TransactionsController.Refresh for why (ADR-116).
            _occurrencesListView.Rebuild();

            _timelineRows = viewModel.Timeline.ToList();
            var hasTimeline = _timelineRows.Count > 0;
            _timelineEmptyLabel.style.display = hasTimeline ? DisplayStyle.None : DisplayStyle.Flex;
            _timelineListView.style.display = hasTimeline ? DisplayStyle.Flex : DisplayStyle.None;
            _timelineListView.itemsSource = _timelineRows;
            _timelineListView.Rebuild();
        }

        private void RebuildAccountChoices()
        {
            if (_accountOptions.Count == 0)
            {
                // Unlike every other screen's account picker, this one stays visible even with
                // zero accounts (it is not gated behind an already-disabled "create" button) —
                // an empty DropdownField renders as blank text with no explanation, so it needs
                // its own placeholder rather than falling back to string.Empty.
                _accountField.choices = new List<string> { "Aucun compte" };
                _accountField.SetValueWithoutNotify("Aucun compte");
                _accountField.SetEnabled(false);
                _simulationRunButton.SetEnabled(false);
                return;
            }

            _accountField.SetEnabled(true);
            var choices = _accountOptions.Select(o => o.Name).ToList();
            _accountField.choices = choices;

            var index = _selectedAccountId is int id ? _accountOptions.ToList().FindIndex(o => o.Id == id) : -1;
            _accountField.SetValueWithoutNotify(index >= 0 && index < choices.Count ? choices[index] : string.Empty);

            _simulationRunButton.SetEnabled(_selectedAccountId is not null);
        }

        private void RebuildCategoryChoices()
        {
            var choices = new List<string> { "Aucune" };
            choices.AddRange(_categoryOptions.Select(c => c.Name));
            _simulationCategoryField.choices = choices;
            if (string.IsNullOrEmpty(_simulationCategoryField.value))
            {
                _simulationCategoryField.SetValueWithoutNotify(choices[0]);
            }
        }

        private void RenderSynthesis(ForecastSynthesisViewModel? synthesis)
        {
            _currentValueLabel.text = synthesis?.CurrentBalanceText ?? "—";
            _projectedValueLabel.text = synthesis?.ProjectedBalanceText ?? "—";
            _lowestValueLabel.text = synthesis?.LowestBalanceText ?? "—";
            _lowestDateLabel.text = synthesis?.LowestBalanceDateText ?? "—";
            _incomeValueLabel.text = synthesis?.ExpectedIncomeText ?? "—";
            _expensesValueLabel.text = synthesis?.ExpectedExpensesText ?? "—";

            _warningsList.Clear();
            foreach (var warning in synthesis?.Warnings ?? Array.Empty<string>())
            {
                var label = new Label(warning);
                label.AddToClassList("empty-state");
                _warningsList.Add(label);
            }
        }

        private void RenderVerificationQueue(IReadOnlyList<VerificationRowViewModel> queue)
        {
            _verificationCountLabel.text = queue.Count.ToString();
            _verificationEmptyLabel.style.display = queue.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            _verificationList.Clear();
            foreach (var row in queue)
            {
                _verificationList.Add(BuildVerificationRow(row));
            }

            if (_confirmingOccurrenceId is int id && queue.All(r => r.OccurrenceId != id))
            {
                CloseConfirmForm();
            }
        }

        private VisualElement BuildVerificationRow(VerificationRowViewModel row)
        {
            var element = new VisualElement();
            element.AddToClassList("verification-row");
            if (row.IsMissed)
            {
                element.AddToClassList("account-row-archived");
            }

            var textColumn = new VisualElement { style = { flexGrow = 1 } };
            var labelElement = new Label(row.IsMissed ? $"{row.Label} (manquée)" : row.Label);
            labelElement.AddToClassList("verification-row-label");
            var dateElement = new Label(row.DateText);
            dateElement.AddToClassList("verification-row-date");
            textColumn.Add(labelElement);
            textColumn.Add(dateElement);

            var amountElement = new Label(row.AmountText);
            amountElement.AddToClassList("verification-row-amount");

            var actions = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var confirmButton = new Button(() => OpenConfirmForm(row)) { text = "C'est arrivé" };
            confirmButton.AddToClassList("secondary-button");
            var cancelButton = new Button(() => CancelOccurrence(row.OccurrenceId)) { text = "Annuler" };
            cancelButton.AddToClassList("secondary-button");
            actions.Add(confirmButton);
            actions.Add(cancelButton);

            element.Add(textColumn);
            element.Add(amountElement);
            element.Add(actions);
            return element;
        }

        private void OpenConfirmForm(VerificationRowViewModel row)
        {
            _confirmingOccurrenceId = row.OccurrenceId;
            _confirmingIsNegative = row.ExpectedAmountMinor < 0;

            _confirmFormTitle.text = row.Label;
            _confirmDateField.SetValueWithoutNotify(DateFormat.ForInput(row.ExpectedDate));
            _confirmAmountField.SetValueWithoutNotify(PlainAmountText(Math.Abs(row.ExpectedAmountMinor)));
            HideConfirmError();

            _confirmFormCard.style.display = DisplayStyle.Flex;
        }

        private void CloseConfirmForm()
        {
            _confirmFormCard.style.display = DisplayStyle.None;
            _confirmingOccurrenceId = null;
        }

        private void SubmitConfirm()
        {
            if (_confirmingOccurrenceId is not int occurrenceId)
            {
                return;
            }

            if (!MoneyFormat.TryParseEurosToMinor(_confirmAmountField.value, out var magnitude) || magnitude <= 0)
            {
                ShowConfirmError("Le montant doit être un nombre positif, ex. 45,90.");
                return;
            }

            if (!DateFormat.TryParseInput(_confirmDateField.value, out var date))
            {
                ShowConfirmError("La date doit être au format jj/mm/aaaa.");
                return;
            }

            var signedAmount = _confirmingIsNegative ? -Math.Abs(magnitude) : Math.Abs(magnitude);
            _app.ForecastOccurrences.ConfirmAsTransaction(occurrenceId, date, signedAmount);

            CloseConfirmForm();
            Refresh();
        }

        private void CancelOccurrence(int occurrenceId)
        {
            _app.ForecastOccurrences.Cancel(occurrenceId);
            if (_confirmingOccurrenceId == occurrenceId)
            {
                CloseConfirmForm();
            }

            Refresh();
        }

        private void OnAccountChanged()
        {
            var index = _accountField.index;
            if (index < 0 || index >= _accountOptions.Count)
            {
                return;
            }

            _selectedAccountId = _accountOptions[index].Id;
            CloseConfirmForm();
            Refresh();
        }

        /// <summary>Collapsed by default — this screen already stacks five other cards, and the
        /// simulation is the one section nobody needs on every visit. See docs/07-Interface.md §3.</summary>
        private void ToggleSimulationBody()
        {
            _simulationExpanded = !_simulationExpanded;
            _simulationBody.style.display = _simulationExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            _simulationToggleButton.text = _simulationExpanded ? "Masquer" : "Simuler un scénario";
        }

        private void RunSimulation()
        {
            if (_selectedAccountId is not int accountId)
            {
                return;
            }

            var label = _simulationLabelField.value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(label))
            {
                ShowSimulationError("Le libellé est requis.");
                return;
            }

            if (!MoneyFormat.TryParseEurosToMinor(_simulationAmountField.value, out var magnitude) || magnitude <= 0)
            {
                ShowSimulationError("Le montant doit être un nombre positif, ex. 45,90.");
                return;
            }

            if (!DateFormat.TryParseInput(_simulationDateField.value, out var date))
            {
                ShowSimulationError("La date doit être au format jj/mm/aaaa.");
                return;
            }

            var signedAmount = _simulationTypeField.value == SimulationTypeOptions[0] ? -magnitude : magnitude;
            var categoryIndex = _simulationCategoryField.index;
            var categoryId = categoryIndex <= 0 ? (int?)null : _categoryOptions[categoryIndex - 1].Id;

            var today = DateTime.Now;
            var horizonDays = _app.Settings.Get().ForecastHorizonDays;
            var result = _app.Forecast.Simulate(
                accountId, today, today.AddDays(horizonDays), today, label, date, signedAmount, categoryId);

            var account = _app.Accounts.FindById(accountId);
            var currency = account?.Currency ?? "EUR";

            _simulationResultProjectedLabel.text = MoneyFormat.Format(result.ClosingBalanceMinor, currency);
            _simulationResultLowestLabel.text = MoneyFormat.Format(result.LowestBalanceMinor, currency);
            _simulationResultLowestDateLabel.text = DateFormat.Short(result.LowestBalanceDate);
            HideSimulationError();

            _simulationResultCard.style.display = DisplayStyle.Flex;
        }

        private static string PlainAmountText(long magnitude) => $"{magnitude / 100},{magnitude % 100:D2}";

        private void ShowConfirmError(string message)
        {
            _confirmErrorLabel.text = message;
            _confirmErrorLabel.style.display = DisplayStyle.Flex;
        }

        private void HideConfirmError()
        {
            _confirmErrorLabel.text = string.Empty;
            _confirmErrorLabel.style.display = DisplayStyle.None;
        }

        private void ShowSimulationError(string message)
        {
            _simulationErrorLabel.text = message;
            _simulationErrorLabel.style.display = DisplayStyle.Flex;
        }

        private void HideSimulationError()
        {
            _simulationErrorLabel.text = string.Empty;
            _simulationErrorLabel.style.display = DisplayStyle.None;
        }
    }
}
