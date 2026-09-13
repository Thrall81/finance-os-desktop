using System;
using System.IO;

namespace FinanceOS.App
{
    /// <summary>
    /// Manual, on-demand backup — "export/sauvegarde manuelle" per docs/01-Perimetre.md §2.11.
    /// A plain file copy: safe for a user-initiated action taken between writes (this app never
    /// holds a long-lived transaction open outside a single Insert/Update call), not a hot
    /// backup mechanism. JSON export (the doc's other option) is not built — a single SQLite file
    /// copy already satisfies "copie du fichier SQLite ou export JSON" on its own.
    /// </summary>
    public sealed class BackupService
    {
        private const string BackupsFolderName = "backups";

        private readonly string _databasePath;

        public BackupService(string databasePath) => _databasePath = databasePath;

        public string CreateBackup(DateTimeOffset? now = null)
        {
            var directory = Path.GetDirectoryName(_databasePath);
            var backupsDirectory = string.IsNullOrEmpty(directory)
                ? BackupsFolderName
                : Path.Combine(directory, BackupsFolderName);
            Directory.CreateDirectory(backupsDirectory);

            var timestamp = (now ?? DateTimeOffset.Now).ToString("yyyyMMdd-HHmmss");
            var fileName = $"financeos-backup-{timestamp}.db";
            var backupPath = Path.Combine(backupsDirectory, fileName);

            File.Copy(_databasePath, backupPath, overwrite: false);
            return backupPath;
        }
    }
}
