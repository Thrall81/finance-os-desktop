using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Data;
using FinanceOS.Domain;

namespace FinanceOS.App
{
    /// <summary>Orchestrates category creation and lifecycle. See docs/01-Perimetre.md §2.3.</summary>
    public sealed class CategoryService
    {
        private readonly CategoryRepository _categories;

        public CategoryService(CategoryRepository categories) => _categories = categories;

        public Category Create(string name, CategoryType type, int? parentId = null, string? color = null)
        {
            if (parentId is int newCategoryParentId)
            {
                RequireTopLevelParent(newCategoryParentId);
            }

            var category = new Category(name, type, parentId, color);
            return _categories.Insert(category);
        }

        public Category? FindById(int categoryId) => _categories.FindById(categoryId);

        public IReadOnlyList<Category> ListActive() => _categories.ListActive();

        public IReadOnlyList<Category> ListAll() => _categories.ListAll();

        public void Rename(int categoryId, string name)
        {
            var category = RequireCategory(categoryId);
            category.Rename(name);
            _categories.Update(category);
        }

        /// <summary>Enforces "deux niveaux maximum" (docs/01-Perimetre.md §2.3) from both
        /// directions: the target parent must itself be top-level (else this category would become
        /// a grandchild), and this category must not already have subcategories of its own (else
        /// they would become grandchildren). Neither check exists on the domain type itself —
        /// `Category.MoveUnder` just reassigns `ParentId` — because checking "does this category
        /// have children" needs the repository, which the domain layer never touches.</summary>
        public void MoveUnder(int categoryId, int? parentId)
        {
            var category = RequireCategory(categoryId);

            if (parentId is int newParentId)
            {
                if (newParentId == categoryId)
                {
                    throw new ArgumentException("Une catégorie ne peut pas être sa propre catégorie parente.", nameof(parentId));
                }

                RequireTopLevelParent(newParentId);

                if (_categories.ListAll().Any(c => c.ParentId == categoryId))
                {
                    throw new ArgumentException(
                        "Cette catégorie a déjà des sous-catégories : elle ne peut pas devenir elle-même une sous-catégorie (deux niveaux maximum).",
                        nameof(parentId));
                }
            }

            category.MoveUnder(parentId);
            _categories.Update(category);
        }

        private void RequireTopLevelParent(int parentId)
        {
            var parent = _categories.FindById(parentId)
                ?? throw new ArgumentException($"Catégorie parente introuvable (#{parentId}).", nameof(parentId));

            if (parent.ParentId is not null)
            {
                throw new ArgumentException(
                    "Une sous-catégorie ne peut pas elle-même avoir une sous-catégorie (deux niveaux maximum).",
                    nameof(parentId));
            }
        }

        public void Archive(int categoryId)
        {
            var category = RequireCategory(categoryId);
            category.Archive();
            _categories.Update(category);
        }

        public void Restore(int categoryId)
        {
            var category = RequireCategory(categoryId);
            category.Restore();
            _categories.Update(category);
        }

        /// <summary>Populates the starter category list from docs/01-Perimetre.md §2.3 — a
        /// no-op if any category already exists, so it is safe to call unconditionally on first
        /// launch. See docs/07-Interface.md §4 (onboarding).</summary>
        public void SeedDefaultCategoriesIfEmpty()
        {
            if (_categories.ListAll().Count > 0)
            {
                return;
            }

            var defaults = new (string Name, CategoryType Type)[]
            {
                ("Logement", CategoryType.Expense),
                ("Alimentation", CategoryType.Expense),
                ("Transport", CategoryType.Expense),
                ("Santé", CategoryType.Expense),
                ("Loisirs", CategoryType.Expense),
                ("Abonnements", CategoryType.Expense),
                ("Revenus", CategoryType.Income),
                ("Épargne", CategoryType.Savings),
                ("Investissements", CategoryType.Savings),
                ("Virements internes", CategoryType.Transfer),
            };

            foreach (var (name, type) in defaults)
            {
                _categories.Insert(new Category(name, type, isSystem: true));
            }
        }

        private Category RequireCategory(int categoryId) =>
            _categories.FindById(categoryId) ?? throw new InvalidOperationException($"Category #{categoryId} not found.");
    }
}
