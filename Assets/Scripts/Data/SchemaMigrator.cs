using SQLite;

namespace FinanceOS.Data
{
    /// <summary>
    /// Applies pending entries from <see cref="SchemaMigrations"/> in order, tracked by
    /// PRAGMA user_version. Re-running against an up-to-date database is a no-op.
    /// See docs/04-Stack_technique.md §5.3.
    /// </summary>
    internal static class SchemaMigrator
    {
        public static void Apply(SQLiteConnection connection)
        {
            var currentVersion = connection.ExecuteScalar<int>("PRAGMA user_version");
            var migrations = SchemaMigrations.OrderedMigrations;

            for (var version = currentVersion; version < migrations.Length; version++)
            {
                var statements = migrations[version];
                var appliedVersion = version + 1;

                connection.RunInTransaction(() =>
                {
                    foreach (var statement in statements)
                    {
                        connection.Execute(statement);
                    }

                    connection.Execute($"PRAGMA user_version = {appliedVersion}");
                });
            }
        }
    }
}
