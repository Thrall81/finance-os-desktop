using System;

namespace FinanceOS.Domain
{
    /// <summary>
    /// An optional, lightweight third party linked to transactions or recurring operations.
    /// See docs/03-Modele_de_donnees.md §14 and docs/01-Perimetre.md §2.5.
    /// </summary>
    public sealed class Counterparty
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public Counterparty(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Counterparty name is required.", nameof(name));
            }

            Name = name;
        }

        private Counterparty(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public static Counterparty FromStorage(int id, string name) => new(id, name);

        public void AssignId(int id)
        {
            if (Id != 0)
            {
                throw new InvalidOperationException("Counterparty already has an id.");
            }

            Id = id;
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Counterparty name is required.", nameof(name));
            }

            Name = name;
        }
    }
}
