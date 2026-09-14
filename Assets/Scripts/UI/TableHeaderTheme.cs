using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Every MultiColumnListView's header row paints its background as a plain inline Color
    /// (verified via the UI Toolkit Debugger on the "unity-multi-column-header__column" element —
    /// "background-color: inline", not "unity stylesheet") from Unity's own default runtime
    /// theme, computed once at construction. No USS rule can override an inline style regardless
    /// of selector specificity, so theme.uss's .theme-dark class can never reach it — confirmed
    /// the hard way: a first attempt targeting ".unity-multi-column-view__header-container" in
    /// theme.uss compiled and parsed fine but changed nothing, because that container's own
    /// background paints underneath its children, not through them. This explicitly overwrites
    /// each header column's inline background after the header actually exists — which, like
    /// everything else about a MultiColumnListView's virtualized content, only happens after a
    /// real layout pass, never during a bare .Instantiate() (ADR-112/113). See ADR-135.
    /// </summary>
    internal static class TableHeaderTheme
    {
        private static readonly Color BorderLight = new(0.882f, 0.874f, 0.827f); // --color-border
        private static readonly Color BorderDark = new(0.169f, 0.184f, 0.216f);

        /// <summary>Registers a one-time GeometryChangedEvent handler that (re-)applies the
        /// header color every time this list view's layout resolves — harmless to repeat (window
        /// resizes, column drags), and the only reliable point at which the header column
        /// elements are guaranteed to exist.</summary>
        public static void Wire(MultiColumnListView listView, bool isDarkTheme)
        {
            listView.RegisterCallback<GeometryChangedEvent>(_ => Apply(listView, isDarkTheme));
        }

        private static void Apply(MultiColumnListView listView, bool isDarkTheme)
        {
            var color = isDarkTheme ? BorderDark : BorderLight;
            listView.Query<VisualElement>(className: "unity-multi-column-header__column").ForEach(
                column => column.style.backgroundColor = color);
        }
    }
}
