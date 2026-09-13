using System.Collections.Generic;
using FinanceOS.Domain;

namespace FinanceOS.UI
{
    /// <summary>One row in the categories list — already formatted, see docs/07-Interface.md §3.
    /// Carries the raw <see cref="CategoryType"/> and <see cref="ParentId"/> alongside their
    /// display text so the edit form can filter/preselect the parent dropdown without re-parsing
    /// text.</summary>
    public sealed record CategoryRowViewModel(
        int Id, string Name, CategoryType Type, string TypeText, int? ParentId, string ParentText, bool IsSystem, bool IsArchived);

    /// <summary>One selectable parent in the create/edit form's "Catégorie parente" dropdown — only
    /// ever a top-level category (docs/01-Perimetre.md §2.3's "deux niveaux maximum"), carrying its
    /// raw <see cref="CategoryType"/> rather than display text so the controller can filter the
    /// list down to the type currently selected in the form, the same way
    /// RecurringOperationsController filters its account dropdowns by the chosen operation type.</summary>
    public sealed record CategoryParentOptionViewModel(int Id, string Name, CategoryType Type);

    public sealed record CategoriesViewModel(
        IReadOnlyList<CategoryRowViewModel> Categories,
        IReadOnlyList<CategoryParentOptionViewModel> ParentOptions);
}
