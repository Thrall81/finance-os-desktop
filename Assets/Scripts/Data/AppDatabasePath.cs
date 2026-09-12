using System.IO;
using UnityEngine;

namespace FinanceOS.Data
{
    /// <summary>
    /// Resolves where the single SQLite file lives: a portable "data" folder next to the
    /// executable when it is writable, otherwise the standard per-user AppData location.
    /// See docs/08-Confidentialite_et_donnees.md §2.
    /// </summary>
    public static class AppDatabasePath
    {
        private const string FileName = "financeos.db";
        private const string PortableFolderName = "data";
        private const string ProbeFileName = ".write-check";

        public static string Resolve()
        {
            var portableDirectory = Path.Combine(GetApplicationDirectory(), PortableFolderName);
            if (IsWritableDirectory(portableDirectory))
            {
                return Path.Combine(portableDirectory, FileName);
            }

            var appDataDirectory = Application.persistentDataPath;
            Directory.CreateDirectory(appDataDirectory);
            return Path.Combine(appDataDirectory, FileName);
        }

        private static string GetApplicationDirectory()
        {
            var dataPath = Application.dataPath;
            return Path.GetDirectoryName(dataPath) ?? dataPath;
        }

        private static bool IsWritableDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var probePath = Path.Combine(directory, ProbeFileName);
                File.WriteAllText(probePath, string.Empty);
                File.Delete(probePath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
