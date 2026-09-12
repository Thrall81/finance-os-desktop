using System.Collections.Generic;
using System.Linq;
using FinanceOS.Domain;
using SQLite;

namespace FinanceOS.Data
{
    internal sealed class CategoryRow
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Color { get; set; }
        public bool IsSystem { get; set; }
        public bool IsArchived { get; set; }
    }

    /// <summary>Maps <see cref="Category"/> to and from the `category` table.</summary>
    public sealed class CategoryRepository
    {
        private const string SelectColumns =
            @"SELECT id AS Id, parent_id AS ParentId, name AS Name, type AS Type, color AS Color,
                     is_system AS IsSystem, is_archived AS IsArchived
              FROM category";

        private readonly SQLiteConnection _connection;

        public CategoryRepository(SQLiteConnection connection) => _connection = connection;

        public Category Insert(Category category)
        {
            _connection.Execute(
                "INSERT INTO category (parent_id, name, type, color, is_system, is_archived) VALUES (?, ?, ?, ?, ?, ?)",
                category.ParentId,
                category.Name,
                category.Type.ToStorageString(),
                category.Color,
                category.IsSystem,
                category.IsArchived);

            var id = (int)_connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            category.AssignId(id);
            return category;
        }

        public void Update(Category category)
        {
            _connection.Execute(
                "UPDATE category SET parent_id = ?, name = ?, type = ?, color = ?, is_archived = ? WHERE id = ?",
                category.ParentId,
                category.Name,
                category.Type.ToStorageString(),
                category.Color,
                category.IsArchived,
                category.Id);
        }

        public Category? FindById(int id)
        {
            var row = _connection.Query<CategoryRow>($"{SelectColumns} WHERE id = ?", id).FirstOrDefault();
            return row is null ? null : Map(row);
        }

        public IReadOnlyList<Category> ListActive() =>
            _connection.Query<CategoryRow>($"{SelectColumns} WHERE is_archived = 0 ORDER BY name").Select(Map).ToList();

        public IReadOnlyList<Category> ListAll() =>
            _connection.Query<CategoryRow>($"{SelectColumns} ORDER BY name").Select(Map).ToList();

        private static Category Map(CategoryRow row) => Category.FromStorage(
            row.Id, row.ParentId, row.Name, StorageFormat.ParseCategoryType(row.Type), row.Color, row.IsSystem, row.IsArchived);
    }
}
