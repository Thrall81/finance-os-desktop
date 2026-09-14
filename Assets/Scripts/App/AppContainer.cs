using System;
using FinanceOS.Data;

namespace FinanceOS.App
{
    /// <summary>
    /// The composition root: opens the database, builds every repository once, and builds every
    /// application service on top of them. Plain C# on purpose — testable by direct
    /// instantiation, unlike a MonoBehaviour. A thin MonoBehaviour in FinanceOS.UI will construct
    /// one of these in Awake() once there is a scene to hand it to; there isn't one yet.
    /// See docs/02-Architecture.md §4.
    /// </summary>
    public sealed class AppContainer : IDisposable
    {
        public AppDatabase Database { get; }

        /// <summary>Forwards <see cref="AppDatabase.DatabasePath"/> so callers that must stay
        /// FinanceOS.Data-free (i.e. FinanceOS.UI, per docs/05-Conventions_de_code.md §6) can
        /// read the data file's location without referencing <see cref="AppDatabase"/> itself.</summary>
        public string DatabasePath => Database.DatabasePath;

        public AccountService Accounts { get; }
        public CategoryService Categories { get; }
        public CounterpartyService Counterparties { get; }
        public TransactionService Transactions { get; }
        public InternalTransferService InternalTransfers { get; }
        public TransferDetectionService TransferDetection { get; }
        public RecurringOperationService RecurringOperations { get; }
        public ForecastOccurrenceService ForecastOccurrences { get; }
        public ForecastService Forecast { get; }
        public BudgetService Budget { get; }
        public AppSettingsService Settings { get; }
        public BackupService Backup { get; }

        public AppContainer() : this(AppDatabasePath.Resolve())
        {
        }

        public AppContainer(string databasePath)
        {
            Database = new AppDatabase(databasePath);
            var connection = Database.Connection;

            var accountRepository = new AccountRepository(connection);
            var snapshotRepository = new AccountBalanceSnapshotRepository(connection);
            var categoryRepository = new CategoryRepository(connection);
            var counterpartyRepository = new CounterpartyRepository(connection);
            var transactionRepository = new TransactionRepository(connection);
            var transferLinkRepository = new TransferLinkRepository(connection);
            var recurringOperationRepository = new RecurringOperationRepository(connection);
            var occurrenceRepository = new ForecastOccurrenceRepository(connection);
            var budgetRepository = new BudgetRepository(connection);
            var budgetAllocationRepository = new BudgetAllocationRepository(connection);
            var appSettingsRepository = new AppSettingsRepository(connection);

            Accounts = new AccountService(accountRepository, snapshotRepository);
            Categories = new CategoryService(categoryRepository);
            Counterparties = new CounterpartyService(counterpartyRepository);
            Transactions = new TransactionService(transactionRepository);
            InternalTransfers = new InternalTransferService(transactionRepository, transferLinkRepository);
            TransferDetection = new TransferDetectionService(transactionRepository, transferLinkRepository);
            RecurringOperations = new RecurringOperationService(recurringOperationRepository, occurrenceRepository);
            ForecastOccurrences = new ForecastOccurrenceService(
                occurrenceRepository, transactionRepository, transferLinkRepository, accountRepository);
            Forecast = new ForecastService(accountRepository, transactionRepository, occurrenceRepository);
            Budget = new BudgetService(budgetRepository, budgetAllocationRepository, transactionRepository, occurrenceRepository, categoryRepository);
            Settings = new AppSettingsService(appSettingsRepository);
            Backup = new BackupService(Database.DatabasePath);
        }

        public void Dispose() => Database.Dispose();
    }
}
