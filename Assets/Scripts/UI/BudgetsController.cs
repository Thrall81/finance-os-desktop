using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.App;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Binds Budgets.uxml: month navigation, budget creation (with an optional copy of the
    /// previous month), activate/close, allocations (add/edit-amount/remove) and the month's
    /// overview (reste à vivre, revenus, épargne, taux d'épargne). Holds only BudgetService and
    /// CategoryService, same narrower-dependency choice as AccountsController.
    /// See docs/07-Interface.md §3/§9/§10.
    /// </summary>
    public sealed class BudgetsController
    {
        private readonly BudgetService _budgets;
        private readonly CategoryService _categories;

        private readonly Label _statusBadge;
        private readonly Button _toggleStatusButton;

        private readonly Label _monthLabel;
        private readonly Button _monthPrevButton;
        private readonly Button _monthNextButton;

        private readonly VisualElement _emptyStateCard;
        private readonly Toggle _emptyCopyPreviousToggle;
        private readonly Button _emptyCreateButton;

        private readonly VisualElement _overviewCard;
        private readonly Label _kpiRemainingLabel;
        private readonly Label _kpiIncomeLabel;
        private readonly Label _kpiSavingsLabel;
        private readonly Label _kpiSavingsRateLabel;

        private readonly VisualElement _savingsChartCard;
        private readonly SavingsEvolutionElement _savingsChart;

        private readonly VisualElement _chartCard;
        private readonly BudgetBarChartElement _chart;

        private readonly VisualElement _allocationsCard;
        private readonly Button _newAllocationButton;
        private readonly Label _allocationsEmptyLabel;
        private readonly MultiColumnListView _allocationsListView;

        private readonly VisualElement _formCard;
        private readonly Label _formTitle;
        private readonly VisualElement _categoryRow;
        private readonly DropdownField _categoryField;
        private readonly VisualElement _categoryReadonlyRow;
        private readonly Label _categoryReadonlyLabel;
        private readonly TextField _plannedField;
        private readonly Label _errorLabel;
        private readonly Button _removeButton;
        private readonly Button _cancelButton;
        private readonly Button _submitButton;

        private int _year;
        private int _month;
        private int? _budgetId;
        private bool _isClosed;
        private IReadOnlyList<DropdownOption> _unallocatedCategories = Array.Empty<DropdownOption>();
        private List<BudgetAllocationRowViewModel> _rows = new();

        private int? _editingAllocationId;
        private int? _editingCategoryId;

        public BudgetsController(VisualElement root, BudgetService budgets, CategoryService categories, DateTime? initialDate = null)
        {
            _budgets = budgets;
            _categories = categories;

            var referenceDate = initialDate ?? DateTime.Now;
            _year = referenceDate.Year;
            _month = referenceDate.Month;

            _statusBadge = root.Q<Label>("budget-status-badge");
            _toggleStatusButton = root.Q<Button>("budget-toggle-status-button");

            _monthLabel = root.Q<Label>("month-label");
            _monthPrevButton = root.Q<Button>("month-prev-button");
            _monthNextButton = root.Q<Button>("month-next-button");

            _emptyStateCard = root.Q<VisualElement>("empty-state-card");
            _emptyCopyPreviousToggle = root.Q<Toggle>("empty-copy-previous-toggle");
            _emptyCreateButton = root.Q<Button>("empty-create-button");

            _overviewCard = root.Q<VisualElement>("overview-card");
            _kpiRemainingLabel = root.Q<Label>("kpi-remaining-value");
            _kpiIncomeLabel = root.Q<Label>("kpi-income-value");
            _kpiSavingsLabel = root.Q<Label>("kpi-savings-value");
            _kpiSavingsRateLabel = root.Q<Label>("kpi-savings-rate-value");

            _savingsChartCard = root.Q<VisualElement>("savings-chart-card");
            _savingsChart = new SavingsEvolutionElement();
            _savingsChart.style.flexGrow = 1;
            root.Q<VisualElement>("savings-chart-container").Add(_savingsChart);

            _chartCard = root.Q<VisualElement>("chart-card");
            _chart = new BudgetBarChartElement();
            _chart.style.flexGrow = 1;
            root.Q<VisualElement>("budget-chart-container").Add(_chart);

            _allocationsCard = root.Q<VisualElement>("allocations-card");
            _newAllocationButton = root.Q<Button>("new-allocation-button");
            _allocationsEmptyLabel = root.Q<Label>("allocations-empty");
            _allocationsListView = root.Q<MultiColumnListView>("allocations-list-view");

            _formCard = root.Q<VisualElement>("allocation-form-card");
            _formTitle = root.Q<Label>("allocation-form-title");
            _categoryRow = root.Q<VisualElement>("allocation-form-category-row");
            _categoryField = root.Q<DropdownField>("allocation-form-category");
            _categoryReadonlyRow = root.Q<VisualElement>("allocation-form-category-readonly-row");
            _categoryReadonlyLabel = root.Q<Label>("allocation-form-category-readonly");
            _plannedField = root.Q<TextField>("allocation-form-planned");
            _errorLabel = root.Q<Label>("allocation-form-error");
            _removeButton = root.Q<Button>("allocation-form-remove-button");
            _cancelButton = root.Q<Button>("allocation-form-cancel-button");
            _submitButton = root.Q<Button>("allocation-form-submit-button");

            SetupColumns();

            _monthPrevButton.clicked += GoToPreviousMonth;
            _monthNextButton.clicked += GoToNextMonth;
            _toggleStatusButton.clicked += ToggleStatus;
            _emptyCreateButton.clicked += CreateBudgetForViewedMonth;
            _newAllocationButton.clicked += OpenCreateForm;
            _cancelButton.clicked += CloseForm;
            _submitButton.clicked += SubmitForm;
            _removeButton.clicked += RemoveAllocation;
            _allocationsListView.selectionChanged += _ => OnRowSelected();

            Refresh();
        }

        private void SetupColumns()
        {
            _allocationsListView.columns.Add(BuildColumn("category", "Catégorie", r => r.CategoryName, width: 160, minWidth: 100, stretchable: true));
            _allocationsListView.columns.Add(BuildColumn("planned", "Prévu", r => r.PlannedText, width: 100, minWidth: 85, alignRight: true));
            _allocationsListView.columns.Add(BuildColumn("actual", "Réel", r => r.ActualText, width: 100, minWidth: 85, alignRight: true));
            _allocationsListView.columns.Add(BuildColumn("committed", "Engagé", r => r.CommittedText, width: 100, minWidth: 85, alignRight: true));
            _allocationsListView.columns.Add(new Column
            {
                name = "remaining",
                title = "Restant",
                width = 100,
                minWidth = 85,
                makeCell = () =>
                {
                    var label = new Label();
                    label.AddToClassList("account-row-balance");
                    return label;
                },
                bindCell = (element, index) =>
                {
                    var label = (Label)element;
                    var row = _rows[index];
                    label.text = row.RemainingText;
                    label.EnableInClassList("amount-negative", row.IsOverBudget);
                },
            });
            _allocationsListView.selectionType = SelectionType.Single;
            _allocationsListView.fixedItemHeight = 28;
        }

        private Column BuildColumn(
            string name, string title, Func<BudgetAllocationRowViewModel, string> textSelector,
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
            var viewModel = BudgetsViewModelBuilder.Build(_budgets, _categories, _year, _month);

            _monthLabel.text = viewModel.MonthLabel;
            _unallocatedCategories = viewModel.UnallocatedCategories;
            _budgetId = viewModel.BudgetId;
            _isClosed = viewModel.IsClosed;
            _rows = viewModel.Allocations.ToList();

            _emptyStateCard.style.display = viewModel.BudgetExists ? DisplayStyle.None : DisplayStyle.Flex;
            _overviewCard.style.display = viewModel.BudgetExists ? DisplayStyle.Flex : DisplayStyle.None;
            _savingsChartCard.style.display = viewModel.BudgetExists ? DisplayStyle.Flex : DisplayStyle.None;
            _chartCard.style.display = viewModel.BudgetExists ? DisplayStyle.Flex : DisplayStyle.None;
            _allocationsCard.style.display = viewModel.BudgetExists ? DisplayStyle.Flex : DisplayStyle.None;

            _savingsChart.Points = viewModel.SavingsEvolution;
            _chart.Groups = viewModel.ChartGroups;

            _statusBadge.style.display = viewModel.BudgetExists ? DisplayStyle.Flex : DisplayStyle.None;
            _toggleStatusButton.style.display = viewModel.BudgetExists ? DisplayStyle.Flex : DisplayStyle.None;
            if (viewModel.BudgetExists)
            {
                _statusBadge.text = viewModel.IsClosed ? "Clôturé" : "Actif";
                _toggleStatusButton.text = viewModel.IsClosed ? "Réouvrir" : "Clôturer";
            }

            _kpiRemainingLabel.text = viewModel.Overview?.RemainingToLiveText ?? "—";
            _kpiIncomeLabel.text = viewModel.Overview?.IncomeText ?? "—";
            _kpiSavingsLabel.text = viewModel.Overview?.SavingsText ?? "—";
            _kpiSavingsRateLabel.text = viewModel.Overview?.SavingsRateText ?? "—";

            _newAllocationButton.SetEnabled(_unallocatedCategories.Count > 0);

            var hasRows = _rows.Count > 0;
            _allocationsEmptyLabel.style.display = hasRows ? DisplayStyle.None : DisplayStyle.Flex;
            _allocationsListView.style.display = hasRows ? DisplayStyle.Flex : DisplayStyle.None;
            _allocationsListView.itemsSource = _rows;
            // Rebuild, not just RefreshItems — see TransactionsController.Refresh for why (ADR-116).
            _allocationsListView.Rebuild();

            CloseForm();
        }

        private void GoToPreviousMonth()
        {
            if (_month == 1)
            {
                _month = 12;
                _year -= 1;
            }
            else
            {
                _month -= 1;
            }

            Refresh();
        }

        private void GoToNextMonth()
        {
            if (_month == 12)
            {
                _month = 1;
                _year += 1;
            }
            else
            {
                _month += 1;
            }

            Refresh();
        }

        private void ToggleStatus()
        {
            if (_budgetId is not int id)
            {
                return;
            }

            if (_isClosed)
            {
                _budgets.Reopen(id);
            }
            else
            {
                _budgets.Close(id);
            }

            Refresh();
        }

        private void CreateBudgetForViewedMonth()
        {
            _budgets.GetOrCreate(_year, _month, copyFromPreviousMonth: _emptyCopyPreviousToggle.value);
            Refresh();
        }

        private void OnRowSelected()
        {
            var index = _allocationsListView.selectedIndex;
            if (index < 0 || index >= _rows.Count)
            {
                return;
            }

            OpenEditForm(_rows[index]);
        }

        private void OpenCreateForm()
        {
            _editingAllocationId = null;
            _editingCategoryId = null;

            _formTitle.text = "Ajouter une catégorie";
            _categoryRow.style.display = DisplayStyle.Flex;
            _categoryReadonlyRow.style.display = DisplayStyle.None;

            var choices = _unallocatedCategories.Select(o => o.Name).ToList();
            _categoryField.choices = choices;
            _categoryField.SetValueWithoutNotify(choices.Count > 0 ? choices[0] : string.Empty);

            _plannedField.SetValueWithoutNotify(string.Empty);
            _removeButton.style.display = DisplayStyle.None;
            _submitButton.text = "Ajouter";
            HideError();

            _formCard.style.display = DisplayStyle.Flex;
        }

        private void OpenEditForm(BudgetAllocationRowViewModel row)
        {
            _editingAllocationId = row.AllocationId;
            _editingCategoryId = row.CategoryId;

            _formTitle.text = row.CategoryName;
            _categoryRow.style.display = DisplayStyle.None;
            _categoryReadonlyRow.style.display = DisplayStyle.Flex;
            _categoryReadonlyLabel.text = row.CategoryName;

            _plannedField.SetValueWithoutNotify(RawAmountText(row.PlannedText));
            _removeButton.style.display = DisplayStyle.Flex;
            _submitButton.text = "Enregistrer";
            HideError();

            _formCard.style.display = DisplayStyle.Flex;
        }

        private void CloseForm()
        {
            _formCard.style.display = DisplayStyle.None;
            _editingAllocationId = null;
            _editingCategoryId = null;
        }

        private void SubmitForm()
        {
            if (_budgetId is not int budgetId)
            {
                return;
            }

            if (!MoneyFormat.TryParseEurosToMinor(_plannedField.value, out var planned) || planned < 0)
            {
                ShowError("Le montant prévu doit être un nombre positif ou nul, ex. 300,00.");
                return;
            }

            int categoryId;
            if (_editingCategoryId is int existingCategoryId)
            {
                categoryId = existingCategoryId;
            }
            else
            {
                if (_categoryField.index < 0 || _categoryField.index >= _unallocatedCategories.Count)
                {
                    ShowError("Choisissez une catégorie.");
                    return;
                }

                categoryId = _unallocatedCategories[_categoryField.index].Id;
            }

            try
            {
                _budgets.UpsertAllocation(budgetId, categoryId, planned);
            }
            catch (ArgumentException ex)
            {
                ShowError(ex.Message);
                return;
            }

            CloseForm();
            Refresh();
        }

        private void RemoveAllocation()
        {
            if (_editingAllocationId is not int allocationId)
            {
                return;
            }

            _budgets.RemoveAllocation(allocationId);
            CloseForm();
            Refresh();
        }

        private static string RawAmountText(string amountText) => amountText.Replace("€", string.Empty).Trim();

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
