using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// Organizes income, expenses and transfers into a two-level hierarchy at most.
    /// See docs/03-Modele_de_donnees.md §12.
    /// </summary>
    public sealed class Category
    {
        public int Id { get; private set; }
        public int? ParentId { get; private set; }
        public string Name { get; private set; }
        public CategoryType Type { get; private set; }
        public string? Color { get; private set; }
        public bool IsSystem { get; }
        public bool IsArchived { get; private set; }

        public Category(string name, CategoryType type, int? parentId = null, string? color = null, bool isSystem = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Category name is required.", nameof(name));
            }

            Name = name;
            Type = type;
            ParentId = parentId;
            Color = color;
            IsSystem = isSystem;
            IsArchived = false;
        }

        private Category(int id, int? parentId, string name, CategoryType type, string? color, bool isSystem, bool isArchived)
        {
            Id = id;
            ParentId = parentId;
            Name = name;
            Type = type;
            Color = color;
            IsSystem = isSystem;
            IsArchived = isArchived;
        }

        public static Category FromStorage(
            int id, int? parentId, string name, CategoryType type, string? color, bool isSystem, bool isArchived)
            => new(id, parentId, name, type, color, isSystem, isArchived);

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Category already has an id.");
            }

            Id = id;
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Category name is required.", nameof(name));
            }

            Name = name;
        }

        public void MoveUnder(int? parentId) => ParentId = parentId;

        public void Archive()
        {
            if (IsSystem)
            {
                throw new InvalidOperationException("A system category cannot be archived.");
            }

            IsArchived = true;
        }

        public void Restore() => IsArchived = false;
    }
}
