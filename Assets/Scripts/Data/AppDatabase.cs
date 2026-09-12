using System;
using System.IO;
using SQLite;
using SQLitePCL;

namespace FinanceOS.Data
{
    /// <summary>
    /// Owns the single SQLite connection for the application: resolves the file location,
    /// initializes the native provider once, and brings the schema up to date on open.
    /// See docs/02-Architecture.md §3.2.
    /// </summary>
    public sealed class AppDatabase : IDisposable
    {
        private static bool _providerInitialized;

        public string DatabasePath { get; }

        public SQLiteConnection Connection { get; }

        public AppDatabase() : this(AppDatabasePath.Resolve())
        {
        }

        public AppDatabase(string databasePath)
        {
            EnsureProviderInitialized();

            DatabasePath = databasePath;

            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Connection = new SQLiteConnection(
                databasePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);

            Connection.Execute("PRAGMA foreign_keys = ON");

            SchemaMigrator.Apply(Connection);
        }

        private static void EnsureProviderInitialized()
        {
            if (_providerInitialized)
            {
                return;
            }

            Batteries_V2.Init();
            _providerInitialized = true;
        }

        public void Dispose()
        {
            Connection?.Close();
        }
    }
}
