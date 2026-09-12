using System;
using System.Collections.Generic;
using System.Linq;
using FinanceOS.Data;
using FinanceOS.Domain;

namespace FinanceOS.App
{
    /// <summary>Orchestrates the lightweight, optional counterparty list.
    /// See docs/01-Perimetre.md §2.5.</summary>
    public sealed class CounterpartyService
    {
        private readonly CounterpartyRepository _counterparties;

        public CounterpartyService(CounterpartyRepository counterparties) => _counterparties = counterparties;

        public IReadOnlyList<Counterparty> ListAll() => _counterparties.ListAll();

        /// <summary>Reuses an existing counterparty with the same name (case-insensitive) rather
        /// than creating a duplicate.</summary>
        public Counterparty FindOrCreateByName(string name)
        {
            var existing = _counterparties.ListAll()
                .FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            return existing ?? _counterparties.Insert(new Counterparty(name));
        }
    }
}
