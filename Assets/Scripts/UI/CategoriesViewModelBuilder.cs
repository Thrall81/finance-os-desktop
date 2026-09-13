using System.Linq;
using FinanceOS.App;
using FinanceOS.Domain;

namespace FinanceOS.UI
{
    /// <summary>
    /// Builds the categories screen's display-ready data from CategoryService alone — narrower
    /// than DashboardViewModelBuilder's full AppContainer dependency, same reasoning as
    /// AccountsViewModelBuilder. See docs/07-Interface.md §3.
    /// </summary>
    public static class CategoriesViewModelBuilder
    {
        public static CategoriesViewModel Build(CategoryService categories)
        {
            var all = categories.ListAll();
            var namesById = all.ToDictionary(c => c.Id, c => c.Name);

            // Groups a category with its parent's name so children sort right after their parent
            // (a top-level category's group key is its own name), then places the parent itself
            // (ParentId null) before its children within that group.
            var rows = all
                .OrderBy(c => c.IsArchived)
                .ThenBy(c => c.ParentId is int parentId && namesById.TryGetValue(parentId, out var parentName) ? parentName : c.Name)
                .ThenBy(c => c.ParentId is null ? 0 : 1)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryRowViewModel(
                    c.Id,
                    c.Name,
                    c.Type,
                    TypeText(c.Type),
                    c.ParentId,
                    c.ParentId is int pid && namesById.TryGetValue(pid, out var pName) ? pName : "—",
                    c.IsSystem,
                    c.IsArchived))
                .ToList();

            var parentOptions = all
                .Where(c => !c.IsArchived && c.ParentId is null)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryParentOptionViewModel(c.Id, c.Name, c.Type))
                .ToList();

            return new CategoriesViewModel(rows, parentOptions);
        }

        public static string TypeText(CategoryType type) => type switch
        {
            CategoryType.Expense => "Dépense",
            CategoryType.Income => "Revenu",
            CategoryType.Savings => "Épargne",
            CategoryType.Transfer => "Virement interne",
            _ => type.ToString(),
        };
    }
}
