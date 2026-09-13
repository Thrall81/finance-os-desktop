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
    /// Draws only the ring — no category names or percentages here, since Painter2D cannot draw
    /// text at all. <see cref="DashboardController"/> builds the accompanying legend rows as real
    /// <see cref="Label"/>s, which is also this chart's answer to "never convey information by
    /// color alone" (§8.4): every color has its category name in text right next to it.
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

        private IReadOnlyList<long> _slices = Array.Empty<long>();

        /// <summary>Raw amounts only — the donut only needs proportions to compute wedge angles,
        /// never display text (that's the caller's legend, not this element's job).</summary>
        public IReadOnlyList<long> Slices
        {
            get => _slices;
            set
            {
                _slices = value ?? Array.Empty<long>();
                MarkDirtyRepaint();
            }
        }

        public ExpenseDonutElement()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var total = _slices.Sum();
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
                var sweep = 2f * Mathf.PI * ((float)_slices[i] / total);
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
