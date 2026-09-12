using System;
using System.IO;
using FinanceOS.Data;
using UnityEditor;
using UnityEngine;

namespace FinanceOS.EditorTools
{
    /// <summary>
    /// Manual, batchmode-runnable proof that the vendored SQLite stack actually works end to
    /// end (native provider loads, schema migrates, round-trips a row) — ahead of the proper
    /// EditMode test suite, which is deferred until Assets/Tests gets its own asmdef.
    /// </summary>
    internal static class DatabaseSmokeTest
    {
        [MenuItem("Finance OS/Run Database Smoke Test")]
        public static void Run()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"financeos-smoke-{Guid.NewGuid():N}.db");

            // Deliberately not wrapped in try/finally: a cleanup failure must never mask a
            // real exception from RunAgainst. Leftover temp files are harmless and rare.
            RunAgainst(tempPath);
            TryDeleteQuietly(tempPath);
        }

        private static void TryDeleteQuietly(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                Debug.LogWarning($"[DatabaseSmokeTest] Could not delete temp file (harmless): {path}");
            }
        }

        private static void RunAgainst(string tempPath)
        {
            using var database = new AppDatabase(tempPath);
            var connection = database.Connection;

            connection.Execute(
                "INSERT INTO account (name, type, currency, liquidity_policy, created_at, updated_at) " +
                "VALUES (?, 'current', 'EUR', 'immediate', ?, ?)",
                "Compte courant",
                "2026-09-12T08:00:00+02:00",
                "2026-09-12T08:00:00+02:00");

            var accountId = connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
            var storedName = connection.ExecuteScalar<string>("SELECT name FROM account WHERE id = ?", accountId);

            if (storedName != "Compte courant")
            {
                throw new InvalidOperationException($"Round-trip mismatch: got '{storedName}'.");
            }

            var tableCount = connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'");
            var schemaVersion = connection.ExecuteScalar<int>("PRAGMA user_version");

            Debug.Log(
                $"[DatabaseSmokeTest] OK — schema version {schemaVersion}, {tableCount} tables, " +
                $"account #{accountId} round-tripped correctly. File: {tempPath}");
        }
    }
}
