using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Binds Categories.uxml: list, create, and — in edit mode — rename and re-parent (both always
    /// allowed, even for system categories) plus archive/restore (blocked for system categories,
    /// same guard as `Category.Archive`). Type is creation-only, shown read-only when editing —
    /// same "creation-only fields become read-only" pattern as every other screen's edit form.
    /// Self-contained: rebuilds and re-renders itself after every mutation, same as
    /// AccountsController. Holds only CategoryService. See docs/07-Interface.md §3.
    /// </summary>
    public sealed class CategoriesController
    {
        private const string NoParentChoice = "Aucune (catégorie principale)";

        private static readonly (CategoryType Type, string Text)[] TypeOptions =
        {
            (CategoryType.Expense, CategoriesViewModelBuilder.TypeText(CategoryType.Expense)),
            (CategoryType.Income, CategoriesViewModelBuilder.TypeText(CategoryType.Income)),
            (CategoryType.Savings, CategoriesViewModelBuilder.TypeText(CategoryType.Savings)),
            (CategoryType.Transfer, CategoriesViewModelBuilder.TypeText(CategoryType.Transfer)),
        };

        private readonly CategoryService _categories;

        private readonly Button _newCategoryButton;
        private readonly VisualElement _formCard;
        private readonly Label _formTitle;
        private readonly TextField _nameField;
        private readonly VisualElement _typeRow;
        private readonly DropdownField _typeField;
        private readonly VisualElement _typeReadonlyRow;
        private readonly Label _typeReadonlyLabel;
        private readonly DropdownField _parentField;
        private readonly Label _systemNoteLabel;
        private readonly Label _errorLabel;
        private readonly Button _cancelButton;
        private readonly Button _archiveButton;
        private readonly Button _submitButton;

        private readonly Label _emptyLabel;
        private readonly MultiColumnListView _listView;

        private IReadOnlyList<CategoryParentOptionViewModel> _parentOptions = Array.Empty<CategoryParentOptionViewModel>();
        private List<CategoryParentOptionViewModel> _currentParentChoices = new();
        private List<CategoryRowViewModel> _rows = new();

        private int? _editingCategoryId;
        private bool _editingIsSystem;

        public CategoriesController(VisualElement root, CategoryService categories)
        {
            _categories = categories;

            _newCategoryButton = root.Q<Button>("new-category-button");
            _formCard = root.Q<VisualElement>("category-form-card");
            _formTitle = root.Q<Label>("form-title");
            _nameField = root.Q<TextField>("form-name");
            _typeRow = root.Q<VisualElement>("form-type-row");
            _typeField = root.Q<DropdownField>("form-type");
            _typeReadonlyRow = root.Q<VisualElement>("form-type-readonly-row");
            _typeReadonlyLabel = root.Q<Label>("form-type-readonly");
            _parentField = root.Q<DropdownField>("form-parent");
            _systemNoteLabel = root.Q<Label>("form-system-note");
            _errorLabel = root.Q<Label>("form-error");
            _cancelButton = root.Q<Button>("form-cancel-button");
            _archiveButton = root.Q<Button>("form-archive-button");
            _submitButton = root.Q<Button>("form-submit-button");

            _emptyLabel = root.Q<Label>("categories-empty");
            _listView = root.Q<MultiColumnListView>("categories-list-view");

            _typeField.choices = TypeOptions.Select(o => o.Text).ToList();

            SetupColumns();

            _newCategoryButton.clicked += OpenCreateForm;
            _cancelButton.clicked += CloseForm;
            _submitButton.clicked += SubmitForm;
            _archiveButton.clicked += ToggleArchive;
            _typeField.RegisterValueChangedCallback(_ => RebuildParentChoices(SelectedCreateType(), excludeCategoryId: null));
            _listView.selectionChanged += _ => OnRowSelected();

            Refresh();
        }

        private void SetupColumns()
        {
            _listView.columns.Add(BuildColumn("name", "Nom", r => r.Name, width: 180, minWidth: 120, stretchable: true));
            _listView.columns.Add(BuildColumn("type", "Type", r => r.TypeText, width: 130, minWidth: 100));
            _listView.columns.Add(BuildColumn("parent", "Catégorie parente", r => r.ParentText, width: 160, minWidth: 100));
            _listView.columns.Add(BuildColumn("status", "Statut", StatusText, width: 140, minWidth: 100));
            _listView.selectionType = SelectionType.Single;
            _listView.fixedItemHeight = 28;
        }

        private static string StatusText(CategoryRowViewModel row) =>
            row.IsArchived ? "Archivé" : (row.IsSystem ? "Actif · Système" : "Actif");

        private Column BuildColumn(
            string name, string title, Func<CategoryRowViewModel, string> textSelector, float width, float minWidth, bool stretchable = false)
        {
            return new Column
            {
                name = name,
                title = title,
                width = width,
                minWidth = minWidth,
                stretchable = stretchable,
                makeCell = () => new Label(),
                bindCell = (element, index) => ((Label)element).text = textSelector(_rows[index]),
            };
        }

        public void Refresh()
        {
            var viewModel = CategoriesViewModelBuilder.Build(_categories);

            _parentOptions = viewModel.ParentOptions;
            _rows = viewModel.Categories.ToList();

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
            _editingCategoryId = null;
            _editingIsSystem = false;

            _formTitle.text = "Nouvelle catégorie";
            _nameField.SetValueWithoutNotify(string.Empty);

            _typeRow.style.display = DisplayStyle.Flex;
            _typeReadonlyRow.style.display = DisplayStyle.None;
            _typeField.SetValueWithoutNotify(TypeOptions[0].Text);

            RebuildParentChoices(TypeOptions[0].Type, excludeCategoryId: null);

            _systemNoteLabel.style.display = DisplayStyle.None;
            _archiveButton.style.display = DisplayStyle.None;
            _submitButton.text = "Créer";
            HideError();

            _formCard.style.display = DisplayStyle.Flex;
        }

        private void OpenEditForm(CategoryRowViewModel row)
        {
            _editingCategoryId = row.Id;
            _editingIsSystem = row.IsSystem;

            _formTitle.text = row.Name;
            _nameField.SetValueWithoutNotify(row.Name);

            _typeRow.style.display = DisplayStyle.None;
            _typeReadonlyRow.style.display = DisplayStyle.Flex;
            _typeReadonlyLabel.text = row.TypeText;

            RebuildParentChoices(row.Type, excludeCategoryId: row.Id);
            var currentIndex = row.ParentId is int parentId
                ? _currentParentChoices.FindIndex(o => o.Id == parentId) + 1
                : 0;
            _parentField.SetValueWithoutNotify(_parentField.choices[Math.Max(currentIndex, 0)]);

            _systemNoteLabel.style.display = row.IsSystem ? DisplayStyle.Flex : DisplayStyle.None;
            _archiveButton.style.display = row.IsSystem ? DisplayStyle.None : DisplayStyle.Flex;
            _archiveButton.text = row.IsArchived ? "Restaurer" : "Archiver";
            _submitButton.text = "Enregistrer";
            HideError();

            _formCard.style.display = DisplayStyle.Flex;
        }

        private void CloseForm()
        {
            _formCard.style.display = DisplayStyle.None;
            _editingCategoryId = null;
        }

        /// <summary>Only top-level categories of the given type can be a parent (§2.3's "deux
        /// niveaux maximum" — enforced again, more strictly, in CategoryService.MoveUnder/Create;
        /// this just keeps the dropdown from offering choices that would always be refused), and a
        /// category can never be its own parent.</summary>
        private void RebuildParentChoices(CategoryType type, int? excludeCategoryId)
        {
            _currentParentChoices = _parentOptions
                .Where(o => o.Type == type && o.Id != excludeCategoryId)
                .ToList();

            var choices = new List<string> { NoParentChoice };
            choices.AddRange(_currentParentChoices.Select(o => o.Name));
            _parentField.choices = choices;
            _parentField.SetValueWithoutNotify(choices[0]);
        }

        private CategoryType SelectedCreateType()
        {
            var index = TypeOptions.ToList().FindIndex(o => o.Text == _typeField.value);
            return TypeOptions[index < 0 ? 0 : index].Type;
        }

        private void SubmitForm()
        {
            var name = _nameField.value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                ShowError("Le nom de la catégorie est requis.");
                return;
            }

            var parentIndex = _parentField.index;
            int? parentId = parentIndex <= 0 ? null : _currentParentChoices[parentIndex - 1].Id;

            try
            {
                if (_editingCategoryId is int id)
                {
                    // MoveUnder first: it is the mutation that can actually fail (deux-niveaux
                    // validation) — if it throws, Rename must never have already committed a
                    // partial change the error message would then contradict.
                    _categories.MoveUnder(id, parentId);
                    _categories.Rename(id, name);
                }
                else
                {
                    _categories.Create(name, SelectedCreateType(), parentId);
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

        private void ToggleArchive()
        {
            if (_editingCategoryId is not int id || _editingIsSystem)
            {
                return;
            }

            var category = _categories.FindById(id);
            if (category is null)
            {
                return;
            }

            if (category.IsArchived)
            {
                _categories.Restore(id);
            }
            else
            {
                _categories.Archive(id);
            }

            CloseForm();
            Refresh();
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
