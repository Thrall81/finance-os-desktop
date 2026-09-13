namespace FinanceOS.UI
{
    /// <summary>An id/name pair for a dropdown choice — id kept alongside the display name so a
    /// selection is resolved by index, never by matching the name text back (names are not
    /// guaranteed unique). Shared by every screen with an account/category picker.
    /// See docs/07-Interface.md §9.</summary>
    public sealed record DropdownOption(int Id, string Name);
}
