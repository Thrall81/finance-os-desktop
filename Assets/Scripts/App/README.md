# App

Services applicatifs qui orchestrent Domain + Data (+ Forecast pour `RecurringOperationService`, `ForecastOccurrenceService` et `ForecastService`) : `AccountService`, `CategoryService`, `CounterpartyService`, `TransactionService`, `InternalTransferService`, `RecurringOperationService`, `ForecastOccurrenceService`, `ForecastService`, `BudgetService`, `AppSettingsService`.

`AppContainer` est la racine de composition (construction manuelle, pas de conteneur DI) — voir `docs/02-Architecture.md` §3.3 et §4. Le `MonoBehaviour AppBootstrap` qui l'instanciera au démarrage de la scène n'existe pas encore : rien dans `FinanceOS.UI` n'en a besoin pour l'instant.
