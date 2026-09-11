# Modèle de données

# Finance OS Desktop

---

# 1. Objectif

Adapter le modèle conceptuel de l'ancien projet (`03-Modele_de_donnees.md`) à un stockage SQLite local mono-utilisateur. Les concepts métier ne changent pas ; leur représentation technique se simplifie (plus d'UUID, plus d'entités liées aux connecteurs ou aux documents).

---

# 2. Principes généraux

## 2.1 Identifiants

`INTEGER PRIMARY KEY AUTOINCREMENT` pour toutes les tables (cf. `05-Conventions_de_code.md`, ADR-104). Il n'existe qu'un seul utilisateur implicite : aucune table `User` n'est nécessaire, aucune colonne `user_id` ne pollue les tables.

## 2.2 Montants

Entiers en unités mineures (`INTEGER`, centimes), jamais `REAL`. Convention de signe inchangée : revenu positif, dépense négative.

## 2.3 Dates

Stockées en `TEXT` ISO-8601 (`YYYY-MM-DD` pour une date civile, `YYYY-MM-DDTHH:MM:SS±HH:MM` pour un horodatage).

## 2.4 Suppression

Les tables métier utilisées portent une colonne `is_archived` plutôt qu'une suppression physique, sauf pour les objets jamais utilisés (ex. une catégorie tout juste créée et jamais liée à une transaction).

---

# 3. Entités retenues pour la V1

```text
Account
AccountBalanceSnapshot
Category
Counterparty
Transaction
TransferLink
RecurringOperation
ForecastOccurrence
Budget
BudgetAllocation
AppSetting
```

## Entités volontairement absentes (héritées de l'ancien projet, non pertinentes ici)

```text
User, UserPreference          → un seul utilisateur implicite, pas d'authentification
Document, DocumentExtractedField, DocumentLink   → module Documents hors périmètre (01-Perimetre.md)
ConnectorConnection, ImportRule, ImportRun, ImportedItem   → aucun connecteur
TransactionReconciliation      → rapprochement simplifié directement sur ForecastOccurrence (cf. §11)
Alert                          → alertes calculées à l'affichage, non persistées, en V1
AuditLog                       → pas de besoin d'audit multi-utilisateur pour un usage strictement local
```

---

# 4. Schéma SQL

```sql
CREATE TABLE account (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    institution_name TEXT,
    type TEXT NOT NULL,              -- current | savings | cash | investment | debt
    currency TEXT NOT NULL DEFAULT 'EUR',
    initial_balance_minor INTEGER NOT NULL DEFAULT 0,
    official_balance_minor INTEGER NOT NULL DEFAULT 0,
    official_balance_date TEXT,
    liquidity_policy TEXT NOT NULL,  -- immediate | reserve | excluded
    include_in_net_worth INTEGER NOT NULL DEFAULT 1,
    color TEXT,
    is_archived INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE account_balance_snapshot (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    account_id INTEGER NOT NULL REFERENCES account(id) ON DELETE CASCADE,
    balance_minor INTEGER NOT NULL,
    balance_date TEXT NOT NULL,
    created_at TEXT NOT NULL
);
CREATE INDEX idx_snapshot_account_date ON account_balance_snapshot(account_id, balance_date);

CREATE TABLE category (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    parent_id INTEGER REFERENCES category(id) ON DELETE RESTRICT,
    name TEXT NOT NULL,
    type TEXT NOT NULL,              -- expense | income | savings | transfer
    color TEXT,
    is_system INTEGER NOT NULL DEFAULT 0,
    is_archived INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE counterparty (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL
);

CREATE TABLE transaction_entry (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    account_id INTEGER NOT NULL REFERENCES account(id) ON DELETE CASCADE,
    category_id INTEGER REFERENCES category(id) ON DELETE SET NULL,
    counterparty_id INTEGER REFERENCES counterparty(id) ON DELETE SET NULL,
    recurring_operation_id INTEGER REFERENCES recurring_operation(id) ON DELETE SET NULL,
    amount_minor INTEGER NOT NULL,
    currency TEXT NOT NULL DEFAULT 'EUR',
    operation_date TEXT NOT NULL,
    original_label TEXT NOT NULL,
    normalized_label TEXT,
    source TEXT NOT NULL,            -- manual | csv_import
    is_internal_transfer INTEGER NOT NULL DEFAULT 0,
    is_excluded_from_budget INTEGER NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX idx_transaction_account_date ON transaction_entry(account_id, operation_date);
CREATE INDEX idx_transaction_category_date ON transaction_entry(category_id, operation_date);

CREATE TABLE transfer_link (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    outgoing_transaction_id INTEGER NOT NULL REFERENCES transaction_entry(id) ON DELETE CASCADE,
    incoming_transaction_id INTEGER NOT NULL REFERENCES transaction_entry(id) ON DELETE CASCADE,
    status TEXT NOT NULL,            -- suggested | confirmed | rejected
    created_at TEXT NOT NULL
);

CREATE TABLE recurring_operation (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    type TEXT NOT NULL,              -- income | expense | savings_transfer | internal_transfer
    source_account_id INTEGER REFERENCES account(id) ON DELETE CASCADE,
    destination_account_id INTEGER REFERENCES account(id) ON DELETE CASCADE,
    category_id INTEGER REFERENCES category(id) ON DELETE SET NULL,
    counterparty_id INTEGER REFERENCES counterparty(id) ON DELETE SET NULL,
    expected_amount_minor INTEGER NOT NULL,
    frequency TEXT NOT NULL,         -- weekly | monthly | bimonthly | quarterly | semiannual | yearly
    interval_value INTEGER NOT NULL DEFAULT 1,
    start_date TEXT NOT NULL,
    end_date TEXT,
    expected_day_of_month INTEGER,
    date_tolerance_days INTEGER NOT NULL DEFAULT 3,
    amount_tolerance_minor INTEGER NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    notes TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE forecast_occurrence (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    recurring_operation_id INTEGER REFERENCES recurring_operation(id) ON DELETE CASCADE,
    account_id INTEGER NOT NULL REFERENCES account(id) ON DELETE CASCADE,
    destination_account_id INTEGER REFERENCES account(id) ON DELETE CASCADE,
    category_id INTEGER REFERENCES category(id) ON DELETE SET NULL,
    counterparty_id INTEGER REFERENCES counterparty(id) ON DELETE SET NULL,
    label TEXT NOT NULL,
    expected_date TEXT NOT NULL,
    expected_amount_minor INTEGER NOT NULL,
    status TEXT NOT NULL,            -- planned | matched | cancelled | missed | ignored
    matched_transaction_id INTEGER REFERENCES transaction_entry(id) ON DELETE SET NULL,
    is_manually_adjusted INTEGER NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX idx_occurrence_account_date ON forecast_occurrence(account_id, expected_date);
CREATE INDEX idx_occurrence_recurring ON forecast_occurrence(recurring_operation_id, expected_date);
-- unicité logique : une opération récurrente ne produit pas deux occurrences pour la même échéance
CREATE UNIQUE INDEX uq_occurrence_logical_key
    ON forecast_occurrence(recurring_operation_id, expected_date)
    WHERE recurring_operation_id IS NOT NULL;

CREATE TABLE budget (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    year INTEGER NOT NULL,
    month INTEGER NOT NULL,
    status TEXT NOT NULL DEFAULT 'active',  -- draft | active | closed
    UNIQUE(year, month)
);

CREATE TABLE budget_allocation (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    budget_id INTEGER NOT NULL REFERENCES budget(id) ON DELETE CASCADE,
    category_id INTEGER NOT NULL REFERENCES category(id) ON DELETE CASCADE,
    planned_amount_minor INTEGER NOT NULL,
    UNIQUE(budget_id, category_id)
);

-- table clé/valeur pour les préférences (devise, horizon, seuil, emplacement de données)
CREATE TABLE app_setting (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);

-- version de schéma suivie via PRAGMA user_version, pas de table dédiée
```

---

# 5. Account

Types V1 : `current`, `savings`, `cash`, `investment` (valorisation globale saisie manuellement), `debt`. Politique de liquidité identique à l'ancien projet : `immediate` (courant, espèces), `reserve` (épargne), `excluded` (investissement, dette) — modifiable par l'utilisateur.

---

# 6. Transaction

`source` limité à `manual` et `csv_import` (plus de `bank_connector`, `document_extraction`). Le libellé original importé n'est jamais réécrit ; `normalized_label` porte la correction utilisateur.

---

# 6bis. Catégorisation assistée

Aucune table dédiée : la suggestion (cf. `01-Perimetre.md` §2.4, `07-Interface.md` §7) s'appuie sur une requête simple à la saisie —

```sql
SELECT category_id FROM transaction_entry
WHERE normalized_label = ?
ORDER BY operation_date DESC
LIMIT 1;
```

Le dernier choix de catégorie pour un libellé donné fait office de mémorisation. Pas de table `CategorizationRule` à maintenir.

---

# 7. RecurringOperation → ForecastOccurrence

Logique inchangée par rapport à l'ancien projet : une opération récurrente génère des occurrences de façon idempotente (contrainte d'unicité `recurring_operation_id + expected_date`), une occurrence ajustée manuellement n'est jamais écrasée par une régénération, une occurrence rapprochée (`matched_transaction_id` renseigné) sort du calcul prévisionnel.

Une occurrence **ponctuelle** (achat prévu, prime attendue) est simplement une ligne sans `recurring_operation_id`, comme le recommandait déjà l'ancien modèle (§17.4, solution B).

---

# 8. Budget

Un budget par couple `(year, month)`. Les valeurs réel/engagé/restant sont **calculées à la volée** depuis `transaction_entry` et `forecast_occurrence`, jamais stockées — cohérent avec `34. Données calculées` de l'ancien modèle.

---

# 9. TransferLink

Conservé, simplifié : un virement interne relie deux `transaction_entry` de comptes différents. Détection assistée (montants opposés, dates proches) proposée à l'utilisateur, jamais confirmée automatiquement.

---

# 10. AppSetting

Remplace `UserPreference` : simple table clé/valeur (`default_currency`, `forecast_horizon_days`, `low_balance_threshold_minor`, `data_file_mode`...), lue au démarrage dans un objet `AppSettings` fortement typé côté C#.

---

# 11. Rapprochement simplifié

Pas d'entité `TransactionReconciliation` séparée : le lien direct `forecast_occurrence.matched_transaction_id` suffit pour une utilisation strictement locale et mono-utilisateur, où il n'y a pas besoin de conserver un historique de propositions rejetées à des fins d'audit. Si ce besoin apparaît réellement à l'usage, l'entité pourra être réintroduite.

---

# 12. Ce qui reste identique à l'ancien projet

Le vocabulaire métier, la convention de signe des montants, la distinction stricte réel (`transaction_entry`) / prévu (`forecast_occurrence`), et l'idée que les valeurs agrégées (soldes calculés, synthèses budgétaires) ne sont jamais la seule source de vérité — elles doivent toujours pouvoir être reconstruites depuis les données détaillées.
