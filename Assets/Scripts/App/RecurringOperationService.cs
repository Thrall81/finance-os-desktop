using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Data;
using FinanceOS.Domain;
using FinanceOS.Forecast;

namespace FinanceOS.App
{
    /// <summary>Orchestrates recurring operations and, from them, the forecast occurrences that
    /// actually feed the projection. The only service besides <see cref="ForecastService"/> that
    /// touches FinanceOS.Forecast. See docs/01-Perimetre.md §2.7 and docs/06-Moteur_de_prevision.md §5.</summary>
    public sealed class RecurringOperationService
    {
        private readonly RecurringOperationRepository _operations;
        private readonly ForecastOccurrenceRepository _occurrences;

        public RecurringOperationService(RecurringOperationRepository operations, ForecastOccurrenceRepository occurrences)
        {
            _operations = operations;
            _occurrences = occurrences;
        }

        public RecurringOperation Create(
            string name,
            RecurringOperationType type,
            long expectedAmountMinor,
            RecurringFrequency frequency,
            DateTime startDate,
            int? sourceAccountId = null,
            int? destinationAccountId = null,
            int? categoryId = null,
            int? counterpartyId = null,
            int intervalValue = 1,
            DateTime? endDate = null,
            int? expectedDayOfMonth = null)
        {
            var operation = new RecurringOperation(
                name, type, expectedAmountMinor, frequency, startDate, sourceAccountId, destinationAccountId,
                categoryId, counterpartyId, intervalValue, endDate, expectedDayOfMonth);

            return _operations.Insert(operation);
        }

        public RecurringOperation? FindById(int operationId) => _operations.FindById(operationId);

        public IReadOnlyList<RecurringOperation> ListActive() => _operations.ListActive();

        public IReadOnlyList<RecurringOperation> ListAll() => _operations.ListAll();

        /// <summary>Applies every field the edit form can change — type, accounts, expected
        /// amount, frequency, day-of-month, category, counterparty — in one save, then wipes and
        /// regenerates this operation's still-pending occurrences exactly once. Every one of these
        /// fields is baked into an already-generated ForecastOccurrence at the moment it's created
        /// (see ForecastOccurrenceGenerator), so any of them changing makes existing planned/missed
        /// occurrences stale in the same way ADR-137 first found for accounts alone — consolidated
        /// into a single wipe+regenerate here rather than one per field. The caller is responsible
        /// for regenerating afterward, same as after Create — see the UI README. Name, start date
        /// and interval value stay out of scope: no screen exposes editing them (start date is
        /// immutable in Domain by design; interval value has no form field anywhere).</summary>
        public void UpdateOperation(
            int operationId,
            RecurringOperationType type,
            int? sourceAccountId,
            int? destinationAccountId,
            long expectedAmountMinor,
            RecurringFrequency frequency,
            int? expectedDayOfMonth,
            int? categoryId,
            int? counterpartyId)
        {
            var operation = RequireOperation(operationId);
            operation.ChangeType(type, sourceAccountId, destinationAccountId);
            operation.UpdateExpectedAmount(expectedAmountMinor);
            operation.ChangeSchedule(frequency, expectedDayOfMonth);
            operation.AssignCategory(categoryId);
            operation.AssignCounterparty(counterpartyId);
            _operations.Update(operation);
            _occurrences.DeletePendingForRecurringOperation(operationId);
        }

        /// <summary>Previews the first date this schedule would actually produce, without
        /// persisting anything — lets the UI warn before creation if a start date/day-of-month
        /// mismatch would silently skip the first cycle (e.g. day-of-month 28 with a start date of
        /// the 29th jumps straight to next month). Null means the schedule never produces an
        /// occurrence (e.g. an end date before the start date). See docs/07-Interface.md §3.</summary>
        public DateTime? PreviewFirstOccurrenceDate(
            RecurringFrequency frequency, DateTime startDate, int? expectedDayOfMonth,
            int intervalValue = 1, DateTime? endDate = null) =>
            ForecastOccurrenceGenerator
                .EnumerateScheduledDates(frequency, intervalValue, startDate, endDate, expectedDayOfMonth, startDate, startDate.AddYears(2))
                .Cast<DateTime?>()
                .FirstOrDefault();

        /// <summary>Deletes a recurring operation and every occurrence it generated, refusing if
        /// any of them was ever confirmed as a real transaction — deleting those would silently
        /// erase the record of something that actually happened. Still the only way to undo the
        /// start date itself (immutable by design, see RecurringOperation.StartDate) — every other
        /// field (type, accounts, amount, frequency, day-of-month, category, counterparty) can now
        /// be corrected directly via UpdateOperation instead (ADR-137, extended by ADR-140).
        /// Relies on `forecast_occurrence.recurring_operation_id ON DELETE CASCADE` to remove the
        /// (never reconciled) occurrences themselves. See docs/07-Interface.md §3.</summary>
        public void Delete(int operationId)
        {
            RequireOperation(operationId);
            if (_occurrences.HasMatchedOccurrence(operationId))
            {
                throw new InvalidOperationException(
                    "Impossible de supprimer : au moins une occurrence de cette opération a déjà été confirmée comme réelle.");
            }

            _operations.Delete(operationId);
        }

        public void Suspend(int operationId)
        {
            var operation = RequireOperation(operationId);
            operation.Suspend();
            _operations.Update(operation);
        }

        public void Resume(int operationId)
        {
            var operation = RequireOperation(operationId);
            operation.Resume();
            _operations.Update(operation);
        }

        /// <summary>Generates and persists whatever occurrences are missing for this operation
        /// between <paramref name="from"/> and <paramref name="to"/>. Safe to call repeatedly —
        /// a date already covered is never duplicated.</summary>
        public IReadOnlyList<ForecastOccurrence> GenerateOccurrences(int operationId, DateTime from, DateTime to)
        {
            var operation = RequireOperation(operationId);

            var existingDates = _occurrences
                .ListForRecurringOperation(operationId, operation.StartDate, to)
                .Select(o => o.ExpectedDate)
                .ToList();

            var missing = ForecastOccurrenceGenerator.GenerateMissingOccurrences(operation, from, to, existingDates);

            foreach (var occurrence in missing)
            {
                _occurrences.Insert(occurrence);
            }

            return missing;
        }

        /// <summary>Keeps every active operation's occurrences generated up to today + horizon —
        /// the plumbing that makes "génération idempotente" actually happen, called on app
        /// startup and after creating/resuming an operation. Always backfills from each
        /// operation's own start date: a past-due unconfirmed occurrence is a deliberate state
        /// ("Manquée", see docs/07-Interface.md §6), not a bug to avoid generating.</summary>
        public void GenerateUpcomingOccurrences(DateTime today, int horizonDays)
        {
            var horizonEnd = today.AddDays(horizonDays);
            foreach (var operation in _operations.ListActive())
            {
                GenerateOccurrences(operation.Id, operation.StartDate, horizonEnd);
            }
        }

        private RecurringOperation RequireOperation(int operationId) =>
            _operations.FindById(operationId) ?? throw new InvalidOperationException($"Recurring operation #{operationId} not found.");
    }
}
