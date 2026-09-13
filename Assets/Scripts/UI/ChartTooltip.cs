using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// The one small piece of hover-tooltip plumbing shared by all four charts (§8.4's deferred
    /// "infobulle au survol" requirement, finally built) — creating the floating <see cref="Label"/>
    /// and showing/hiding/positioning it. Each chart still does its own hit-testing (which point,
    /// bar or wedge is under the cursor): that geometry is different for every chart and isn't
    /// worth abstracting, only this presentational part is. See docs/07-Interface.md §8.
    /// </summary>
    internal static class ChartTooltip
    {
        // No layout pass has happened yet when a tooltip first shows, so its real size isn't
        // known — these are conservative estimates used only to keep it from visually spilling
        // past the chart's own edges, not an exact fit.
        private const float EstimatedWidth = 170f;
        private const float EstimatedHeight = 22f;
        private const float OffsetX = 12f;
        private const float OffsetY = -28f;

        public static Label Create(VisualElement parent)
        {
            var tooltip = new Label();
            tooltip.AddToClassList("chart-tooltip");
            tooltip.style.display = DisplayStyle.None;
            tooltip.style.position = Position.Absolute;
            tooltip.pickingMode = PickingMode.Ignore;
            parent.Add(tooltip);
            return tooltip;
        }

        public static void Show(Label tooltip, Rect containerRect, string text, Vector2 pointerPosition)
        {
            tooltip.text = text;
            tooltip.style.display = DisplayStyle.Flex;

            var left = Mathf.Clamp(pointerPosition.x + OffsetX, 0f, Mathf.Max(0f, containerRect.width - EstimatedWidth));
            var top = Mathf.Clamp(pointerPosition.y + OffsetY, 0f, Mathf.Max(0f, containerRect.height - EstimatedHeight));
            tooltip.style.left = left;
            tooltip.style.top = top;
        }

        public static void Hide(Label tooltip) => tooltip.style.display = DisplayStyle.None;
    }
}
