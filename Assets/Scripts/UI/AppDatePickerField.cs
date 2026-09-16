using System;
using Unity.AppUI.Core;
using Unity.AppUI.UI;
using UnityEngine;
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
            Popover currentPopover = null;

            trigger.clicked += () =>
            {
                // TEMPORARY diagnostic logging (ADR-145 essai) — two real fixes (re-entrancy,
                // stylesheet path) haven't resolved "nothing visible happens, no console error"
                // reported from real testing. Logging every stage so the next report pinpoints
                // exactly where it actually stops, instead of guessing a third blind fix. Remove
                // once the calendar is confirmed visible.
                Debug.Log("[AppDatePickerField] click received");

                if (currentPopover is not null)
                {
                    Debug.Log("[AppDatePickerField] ignored — a popover is already tracked as open");
                    return;
                }

                try
                {
                    var picker = new DatePicker { value = new Date(getCurrentValue()) };
                    picker.AddToClassList("appui--medium");
                    picker.AddToClassList(isDarkTheme ? "appui--dark" : "appui--light");
                    Debug.Log("[AppDatePickerField] DatePicker constructed");

                    var popover = Popover.Build(trigger, picker).SetPlacement(PopoverPlacement.BottomStart);
                    Debug.Log($"[AppDatePickerField] Popover built, rootView={popover.rootView}, view.panel={popover.view?.panel}");
                    currentPopover = popover;
                    popover.dismissed += (_, reason) =>
                    {
                        Debug.Log($"[AppDatePickerField] dismissed, reason={reason}");
                        currentPopover = null;
                    };

                    picker.RegisterValueChangedCallback(evt =>
                    {
                        DateTime selected = evt.newValue;
                        trigger.text = DateFormat.ForInput(selected);
                        onChanged(selected);
                        popover.Dismiss();
                    });

                    popover.Show();
                    Debug.Log($"[AppDatePickerField] Show() returned, view.worldBound={popover.view?.worldBound}, "
                        + $"view.resolvedStyle.display={popover.view?.resolvedStyle.display}, view.childCount={popover.view?.childCount}, "
                        + $"picker.worldBound={picker.worldBound}, picker.childCount={picker.childCount}, "
                        + $"picker.resolvedStyle=(w={picker.resolvedStyle.width},h={picker.resolvedStyle.height})");

                    // worldBound can legitimately still be NaN right after Show() if the layout
                    // pass hasn't run yet — checking again a few frames later tells us whether
                    // this is transient (fine) or a persistent, genuine layout failure (not fine).
                    trigger.schedule.Execute(() =>
                    {
                        Debug.Log($"[AppDatePickerField] 200ms later: view.worldBound={popover.view?.worldBound}, "
                            + $"picker.worldBound={picker.worldBound}, picker.childCount={picker.childCount}, "
                            + $"picker.resolvedStyle=(w={picker.resolvedStyle.width},h={picker.resolvedStyle.height}), "
                            + $"picker.panel={picker.panel}");
                    }).ExecuteLater(200);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AppDatePickerField] exception while opening the picker: {ex}");
                    currentPopover = null;
                }
            };
        }
    }
}
