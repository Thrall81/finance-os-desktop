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
        /// app's own <c>.theme-dark</c> toggle and needs its theme applied directly.
        /// <paramref name="appUiThemeStyleSheet"/> (App UI.tss, wired via AppBootstrap/
        /// SceneWiringTools) is added directly to the popover's own root element for the same
        /// reason: a real bug found via the UI Toolkit Debugger, not guessed — the Popover is a
        /// sibling of this screen's own UXML tree at the panel root, not a descendant of it, so a
        /// stylesheet referenced via a UXML <c>&lt;Style src&gt;</c> (this project's usual pattern)
        /// never reaches it no matter how correct its path is. Every element inside the popover had
        /// its full expected structure (DatePicker/YearPicker/MonthPicker/DayPicker, right classes)
        /// but no background-color ever resolved — confirming the stylesheet, not the structure or
        /// sizing, was the actual blocker. Null is accepted (renders unstyled) so a caller that
        /// hasn't wired the asset yet doesn't hard-fail.</summary>
        public static void Attach(
            Button trigger, Func<DateTime> getCurrentValue, Action<DateTime> onChanged, bool isDarkTheme,
            StyleSheet? appUiThemeStyleSheet)
        {
            // A real bug caught by the user clicking the trigger repeatedly, not anticipated:
            // every click built and showed a brand-new Popover with no guard against one already
            // being open (or mid-dismiss animation) — each additional popover kept tracking its
            // own anchor position against `trigger` every layout pass, and enough of them piling
            // up made UI Toolkit's layout solver give up ("Layout update is struggling to process
            // current layout... consider simplifying to avoid recursive layout") and the calendar
            // itself render empty/broken. `AnchorPopup.dismissed` (fires once the popup — including
            // its dismiss animation — has actually finished closing, not just when Dismiss() is
            // called) is what makes it safe to track "is one already live" rather than guessing
            // when it's truly gone.
            Popover? currentPopover = null;

            trigger.clicked += () =>
            {
                if (currentPopover is not null)
                {
                    return;
                }

                var picker = new DatePicker { value = new Date(getCurrentValue()) };
                picker.AddToClassList("appui--medium");
                picker.AddToClassList(isDarkTheme ? "appui--dark" : "appui--light");

                var popover = Popover.Build(trigger, picker).SetPlacement(PopoverPlacement.BottomStart);
                if (appUiThemeStyleSheet is not null && popover.view is not null)
                {
                    popover.view.styleSheets.Add(appUiThemeStyleSheet);
                }

                currentPopover = popover;
                popover.dismissed += (_, _) => currentPopover = null;

                picker.RegisterValueChangedCallback(evt =>
                {
                    DateTime selected = evt.newValue;
                    trigger.text = DateFormat.ForInput(selected);
                    onChanged(selected);
                    popover.Dismiss();
                });

                popover.Show();

                // A real z-order bug found via the UI Toolkit Debugger, on the user's own
                // hypothesis: PopoverVisualElement lands as the FIRST child of the panel root,
                // ahead of UIDocumentRootElement (this whole app's own tree) — in UI Toolkit,
                // draw order follows child order, so the entire app was painting over the popover,
                // not the other way around, even though the popover had correct content and
                // styling by this point. Explicitly moving it to the end of its parent's children
                // (the standard meaning of BringToFront) puts it back on top.
                popover.view?.BringToFront();
            };
        }
    }
}
