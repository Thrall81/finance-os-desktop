using System;
using Unity.AppUI.Core;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FinanceOS.UI
{
    /// <summary>
    /// Wires a field-styled trigger element to open Unity's App UI DatePicker
    /// (com.unity.dt.app-ui, ADR-145) in a Popover anchored to it, replacing free-text jj/mm/aaaa
    /// entry. <see cref="Popover"/> falls back to the enclosing UIDocument's own root visual
    /// element when no App UI <c>Panel</c> ancestor exists — confirmed by reading
    /// Popup.GetRootPopupLayer's actual source in the package cache, not assumed — so this works
    /// inside our plain UIDocument setup without wrapping the whole app in App UI's own Panel.
    /// The trigger is a Button (styled to look like a field, not a button) rather than a bare
    /// VisualElement specifically so the click is guaranteed to fire — same "don't trust an
    /// unverified low-level event on an arbitrary element" caution as everywhere else in this
    /// project when no live panel is available here to test the real interaction. App UI has its
    /// own <c>Unity.AppUI.UI.Button</c>, ambiguous with UI Toolkit's own <c>Button</c> the instant
    /// both namespaces are in scope (a real CS0104 hit, not a hypothetical) — aliased explicitly
    /// below to keep using the plain UI Toolkit one, which is all this project uses elsewhere.
    /// </summary>
    public static class AppDatePickerField
    {
        /// <summary>Wires <paramref name="trigger"/> (its text doubles as the display) to open a
        /// calendar popover seeded from <paramref name="getCurrentValue"/>; <paramref name="onChanged"/>
        /// fires with the newly picked date, and the trigger's own text updates to match.
        /// <paramref name="isDarkTheme"/> picks which App UI theme context class
        /// (<c>appui--dark</c>/<c>appui--light</c>) the popover's content carries — the Popover is
        /// appended to the panel's own root, not under <c>.shell-root</c>, so it never inherits this
        /// app's own <c>.theme-dark</c> toggle and needs its theme applied directly.</summary>
        public static void Attach(Button trigger, Func<DateTime> getCurrentValue, Action<DateTime> onChanged, bool isDarkTheme)
        {
            trigger.clicked += () =>
            {
                var picker = new DatePicker { value = new Date(getCurrentValue()) };
                picker.AddToClassList("appui--medium");
                picker.AddToClassList(isDarkTheme ? "appui--dark" : "appui--light");

                var popover = Popover.Build(trigger, picker).SetPlacement(PopoverPlacement.BottomStart);

                picker.RegisterValueChangedCallback(evt =>
                {
                    DateTime selected = evt.newValue;
                    trigger.text = DateFormat.ForInput(selected);
                    onChanged(selected);
                    popover.Dismiss();
                });

                popover.Show();
            };
        }
    }
}
