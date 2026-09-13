using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// The expense-breakdown donut — a hand-drawn <see cref="Painter2D"/> ring, same family as
    /// <see cref="LineChartElement"/>/<see cref="BudgetBarChartElement"/> (ADR-103). Each wedge is
    /// a filled polygon approximating an arc (outer edge, then back along the inner edge) built
    /// from only <c>MoveTo</c>/<c>LineTo</c> — deliberately avoiding <c>Painter2D.Arc</c>, same
    /// reasoning as the line chart's diamond marker (ADR-119): higher confidence with API members
    /// already proven elsewhere in this codebase than one never used before.
    /// Draws only the ring — no category names or percentages painted on the canvas, since
    /// Painter2D cannot draw text at all. <see cref="DashboardController"/> builds the
    /// accompanying legend rows as real <see cref="Label"/>s, which is also this chart's answer to
    /// "never convey information by color alone" (§8.4): every color has its category name in
    /// text right next to it. Hover shows the exact category/montant/pourcentage of the wedge
    /// under the cursor in a tooltip (§8.4, ADR-124) — the one piece of text this element does
    /// need to know, hence <see cref="Slices"/> taking the full view model rather than raw amounts.
    /// See docs/07-Interface.md §8.
    /// </summary>
    public sealed class ExpenseDonutElement : VisualElement
    {
        /// <summary>Eight muted, mutually distinguishable colors in the same family as the rest of
        /// the palette (`theme.uss`) — reused by <see cref="DashboardController"/> for the legend
        /// swatches so a slice and its legend row always match. Cycles (index % length) past eight
        /// categories — a known limit for this first version, same posture as every other chart's
        /// "revisit once real usage shows it's a problem."</summary>
        public static readonly Color[] Palette =
        {
            new(0.180f, 0.227f, 0.349f), // navy (--color-accent)
            new(0.663f, 0.463f, 0.184f), // gold (--color-gold)
            new(0.420f, 0.478f, 0.369f), // sage
            new(0.478f, 0.361f, 0.380f), // mauve
            new(0.290f, 0.400f, 0.439f), // teal
            new(0.690f, 0.553f, 0.341f), // ochre
            new(0.337f, 0.361f, 0.400f), // ink-600
            new(0.545f, 0.369f, 0.235f), // clay
        };

        private const float TopPadding = 6f;
        private const float BottomPadding = 6f;
        private const float SidePadding = 6f;
        private const float InnerRadiusRatio = 0.55f;
        private const float MinSegmentAngleRadians = 0.05f;

        private readonly Label _tooltip;
        private IReadOnlyList<ExpenseCategorySliceViewModel> _slices = Array.Empty<ExpenseCategorySliceViewModel>();

        public IReadOnlyList<ExpenseCategorySliceViewModel> Slices
        {
            get => _slices;
            set
            {
                _slices = value ?? Array.Empty<ExpenseCategorySliceViewModel>();
                MarkDirtyRepaint();
            }
        }

        public ExpenseDonutElement()
        {
            generateVisualContent += OnGenerateVisualContent;
            _tooltip = ChartTooltip.Create(this);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerLeaveEvent>(_ => ChartTooltip.Hide(_tooltip));
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            var rect = contentRect;
            var index = FindSliceUnderPointer(_slices, rect.width, rect.height, evt.localPosition.x, evt.localPosition.y);
            if (index is null)
            {
                ChartTooltip.Hide(_tooltip);
                return;
            }

            var slice = _slices[index.Value];
            var text = $"{slice.CategoryName} — {slice.AmountText} ({slice.PercentText})";
            ChartTooltip.Show(_tooltip, rect, text, evt.localPosition);
        }

        /// <summary>Which wedge sits under a given pointer position — pure geometry mirroring
        /// <see cref="OnGenerateVisualContent"/> exactly (same center/radius/starting-angle
        /// convention), so it can be unit-tested directly (batchmode cannot simulate pointer
        /// events). Null outside the ring (inside the hole, or past the outer edge) or when no
        /// wedge is drawn. See docs/07-Interface.md §8.4, ADR-124.</summary>
        public static int? FindSliceUnderPointer(
            IReadOnlyList<ExpenseCategorySliceViewModel> slices, float elementWidth, float elementHeight, float localX, float localY)
        {
            if (slices is null || slices.Count == 0)
            {
                return null;
            }

            var total = slices.Sum(s => s.AmountMinor);
            if (total <= 0)
            {
                return null;
            }

            var drawableWidth = elementWidth - 2 * SidePadding;
            var drawableHeight = elementHeight - TopPadding - BottomPadding;
            if (drawableWidth <= 0 || drawableHeight <= 0)
            {
                return null;
            }

            var outerRadius = Mathf.Min(drawableWidth, drawableHeight) / 2f;
            var innerRadius = outerRadius * InnerRadiusRatio;
            var center = new Vector2(SidePadding + drawableWidth / 2f, TopPadding + drawableHeight / 2f);

            var offset = new Vector2(localX, localY) - center;
            var radius = offset.magnitude;
            if (radius < innerRadius || radius > outerRadius)
            {
                return null;
            }

            const float startAngle = -Mathf.PI / 2f; // top, clockwise — same convention as the draw loop
            var relativeAngle = Mathf.Atan2(offset.y, offset.x) - startAngle;
            if (relativeAngle < 0f)
            {
                relativeAngle += 2f * Mathf.PI;
            }

            var angle = 0f;
            for (var i = 0; i < slices.Count; i++)
            {
                var sweep = 2f * Mathf.PI * ((float)slices[i].AmountMinor / total);
                if (relativeAngle >= angle && relativeAngle < angle + sweep)
                {
                    return i;
                }

                angle += sweep;
            }

            return null;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var total = _slices.Sum(s => s.AmountMinor);
            if (_slices.Count == 0 || total <= 0)
            {
                return;
            }

            var rect = contentRect;
            var drawableWidth = rect.width - 2 * SidePadding;
            var drawableHeight = rect.height - TopPadding - BottomPadding;
            if (drawableWidth <= 0 || drawableHeight <= 0)
            {
                return;
            }

            var outerRadius = Mathf.Min(drawableWidth, drawableHeight) / 2f;
            var innerRadius = outerRadius * InnerRadiusRatio;
            var center = new Vector2(SidePadding + drawableWidth / 2f, TopPadding + drawableHeight / 2f);

            var painter = context.painter2D;
            var angle = -Mathf.PI / 2f; // start at the top, clockwise

            for (var i = 0; i < _slices.Count; i++)
            {
                var sweep = 2f * Mathf.PI * ((float)_slices[i].AmountMinor / total);
                if (sweep > 0f)
                {
                    DrawWedge(painter, center, innerRadius, outerRadius, angle, angle + sweep, Palette[i % Palette.Length]);
                }

                angle += sweep;
            }
        }

        private static void DrawWedge(
            Painter2D painter, Vector2 center, float innerRadius, float outerRadius, float startAngle, float endAngle, Color color)
        {
            var sweep = endAngle - startAngle;
            var segments = Mathf.Max(1, Mathf.CeilToInt(sweep / MinSegmentAngleRadians));

            painter.fillColor = color;
            painter.BeginPath();

            for (var s = 0; s <= segments; s++)
            {
                var t = startAngle + sweep * s / segments;
                var point = center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * outerRadius;
                if (s == 0)
                {
                    painter.MoveTo(point);
                }
                else
                {
                    painter.LineTo(point);
                }
            }

            for (var s = segments; s >= 0; s--)
            {
                var t = startAngle + sweep * s / segments;
                var point = center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * innerRadius;
                painter.LineTo(point);
            }

            painter.ClosePath();
            painter.Fill();
        }
    }
}
