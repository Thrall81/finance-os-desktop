using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// The savings-evolution chart — one bar per month, same <see cref="Painter2D"/> family as
    /// <see cref="LineChartElement"/>/<see cref="BudgetBarChartElement"/>/<see cref="ExpenseDonutElement"/>
    /// (ADR-103). A single series, unlike the grouped budget bars — no "never color alone"
    /// concern here, since there is only ever one color to read. Month labels are real
    /// <see cref="Label"/> children positioned under each bar on <see cref="GeometryChangedEvent"/>,
    /// same reason and same technique as <see cref="BudgetBarChartElement"/>'s category labels:
    /// Painter2D cannot draw text. Hover shows the exact month/montant under the cursor in a
    /// tooltip (§8.4, ADR-124). See docs/07-Interface.md §8.
    /// </summary>
    public sealed class SavingsEvolutionElement : VisualElement
    {
        private static readonly Color BarColor = new(0.420f, 0.478f, 0.369f); // sage, same as ExpenseDonutElement.Palette[2]

        private const float TopPadding = 12f;
        private const float LabelRowHeight = 18f;
        private const float BarGapRatio = 0.3f;

        private readonly Label _tooltip;
        private IReadOnlyList<SavingsEvolutionPointViewModel> _points = Array.Empty<SavingsEvolutionPointViewModel>();
        private readonly List<Label> _monthLabels = new();

        public IReadOnlyList<SavingsEvolutionPointViewModel> Points
        {
            get => _points;
            set
            {
                _points = value ?? Array.Empty<SavingsEvolutionPointViewModel>();
                RebuildMonthLabels();
                MarkDirtyRepaint();
            }
        }

        public SavingsEvolutionElement()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(_ => RepositionMonthLabels());
            _tooltip = ChartTooltip.Create(this);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerLeaveEvent>(_ => ChartTooltip.Hide(_tooltip));
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            var rect = contentRect;
            var index = FindBarUnderPointer(_points, rect.width, rect.height, evt.localPosition.x, evt.localPosition.y);
            if (index is null)
            {
                ChartTooltip.Hide(_tooltip);
                return;
            }

            var point = _points[index.Value];
            var text = $"{point.MonthLabel} — {point.SavingsText}";
            ChartTooltip.Show(_tooltip, rect, text, evt.localPosition);
        }

        /// <summary>Which month's bar sits under a given pointer position — pure geometry
        /// mirroring <see cref="OnGenerateVisualContent"/> exactly, so it can be unit-tested
        /// directly (batchmode cannot simulate pointer events). Null over a gap, an undrawn
        /// (zero-value) bar, or outside every slot. See docs/07-Interface.md §8.4, ADR-124.</summary>
        public static int? FindBarUnderPointer(
            IReadOnlyList<SavingsEvolutionPointViewModel> points, float elementWidth, float elementHeight, float localX, float localY)
        {
            if (points is null || points.Count == 0)
            {
                return null;
            }

            var drawableHeight = elementHeight - TopPadding - LabelRowHeight;
            if (elementWidth <= 0 || drawableHeight <= 0)
            {
                return null;
            }

            var maxValue = points.Select(p => p.SavingsMinor).DefaultIfEmpty(0L).Max();
            if (maxValue <= 0)
            {
                return null;
            }

            var slotWidth = elementWidth / points.Count;
            var index = Mathf.FloorToInt(localX / slotWidth);
            if (index < 0 || index >= points.Count)
            {
                return null;
            }

            var barWidth = slotWidth * (1f - BarGapRatio);
            var barLeftOffset = slotWidth * BarGapRatio / 2f;
            var xInSlot = localX - index * slotWidth;
            if (xInSlot < barLeftOffset || xInSlot > barLeftOffset + barWidth)
            {
                return null;
            }

            var value = points[index].SavingsMinor;
            if (value <= 0)
            {
                return null;
            }

            var barHeight = drawableHeight * (float)((double)value / maxValue);
            var top = TopPadding + drawableHeight - barHeight;
            var bottom = TopPadding + drawableHeight;
            if (localY < top || localY > bottom)
            {
                return null;
            }

            return index;
        }

        private void RebuildMonthLabels()
        {
            foreach (var label in _monthLabels)
            {
                label.RemoveFromHierarchy();
            }

            _monthLabels.Clear();

            foreach (var point in _points)
            {
                var label = new Label(point.MonthLabel);
                label.AddToClassList("chart-axis-label");
                label.style.position = Position.Absolute;
                Add(label);
                _monthLabels.Add(label);
            }

            RepositionMonthLabels();
        }

        private void RepositionMonthLabels()
        {
            var rect = contentRect;
            if (rect.width <= 0 || rect.height <= 0 || _points.Count == 0)
            {
                return;
            }

            var slotWidth = rect.width / _points.Count;

            for (var i = 0; i < _monthLabels.Count; i++)
            {
                var label = _monthLabels[i];
                label.style.left = i * slotWidth;
                label.style.width = slotWidth;
                label.style.top = rect.height - LabelRowHeight;
                label.style.height = LabelRowHeight;
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_points.Count == 0)
            {
                return;
            }

            var rect = contentRect;
            var drawableHeight = rect.height - TopPadding - LabelRowHeight;
            if (rect.width <= 0 || drawableHeight <= 0)
            {
                return;
            }

            var maxValue = _points.Select(p => p.SavingsMinor).DefaultIfEmpty(0L).Max();
            if (maxValue <= 0)
            {
                return;
            }

            var painter = context.painter2D;
            var slotWidth = rect.width / _points.Count;
            var barWidth = slotWidth * (1f - BarGapRatio);
            var barLeftOffset = slotWidth * BarGapRatio / 2f;

            for (var i = 0; i < _points.Count; i++)
            {
                var value = _points[i].SavingsMinor;
                if (value <= 0)
                {
                    continue;
                }

                var barHeight = drawableHeight * (float)((double)value / maxValue);
                var left = i * slotWidth + barLeftOffset;
                var top = TopPadding + drawableHeight - barHeight;
                var bottom = TopPadding + drawableHeight;

                painter.fillColor = BarColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(left, top));
                painter.LineTo(new Vector2(left + barWidth, top));
                painter.LineTo(new Vector2(left + barWidth, bottom));
                painter.LineTo(new Vector2(left, bottom));
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
