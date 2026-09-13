using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// Grouped bar chart for prévu/réel/engagé per category — a hand-drawn <see cref="Painter2D"/>
    /// component, same family as <see cref="LineChartElement"/> (ADR-103, no third-party charting
    /// library). Category names can't be drawn via Painter2D (no text API), so they're real
    /// <see cref="Label"/> children positioned under each bar group instead — Painter2D only draws
    /// the bars themselves. See docs/07-Interface.md §8.2.
    /// </summary>
    public sealed class BudgetBarChartElement : VisualElement
    {
        private static readonly Color PlannedColor = new(0.180f, 0.227f, 0.349f); // --color-accent
        private static readonly Color ActualColor = new(0.663f, 0.463f, 0.184f); // --color-gold
        private static readonly Color CommittedColor = new(0.525f, 0.549f, 0.580f); // --color-ink-400

        private const float TopPadding = 12f;
        private const float LabelRowHeight = 18f;
        private const float GroupGap = 10f;
        private const float BarGap = 2f;

        private IReadOnlyList<BudgetChartBarGroupViewModel> _groups = Array.Empty<BudgetChartBarGroupViewModel>();
        private readonly List<Label> _categoryLabels = new();

        /// <summary>Fixed left-to-right order within every group: prévu, réel, engagé — the way
        /// the three bars are told apart without relying on color alone (§8.4), explained in the
        /// card caption next to this element rather than repeated as an in-chart legend.</summary>
        public IReadOnlyList<BudgetChartBarGroupViewModel> Groups
        {
            get => _groups;
            set
            {
                _groups = value ?? Array.Empty<BudgetChartBarGroupViewModel>();
                RebuildCategoryLabels();
                MarkDirtyRepaint();
            }
        }

        public BudgetBarChartElement()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(_ => RepositionCategoryLabels());
        }

        private void RebuildCategoryLabels()
        {
            foreach (var label in _categoryLabels)
            {
                label.RemoveFromHierarchy();
            }

            _categoryLabels.Clear();

            foreach (var group in _groups)
            {
                var label = new Label(group.CategoryName);
                label.AddToClassList("chart-axis-label");
                label.style.position = Position.Absolute;
                Add(label);
                _categoryLabels.Add(label);
            }

            RepositionCategoryLabels();
        }

        private void RepositionCategoryLabels()
        {
            var rect = contentRect;
            if (rect.width <= 0 || rect.height <= 0 || _groups.Count == 0)
            {
                return;
            }

            var groupWidth = rect.width / _groups.Count;

            for (var i = 0; i < _categoryLabels.Count; i++)
            {
                var label = _categoryLabels[i];
                label.style.left = i * groupWidth;
                label.style.width = groupWidth;
                label.style.top = rect.height - LabelRowHeight;
                label.style.height = LabelRowHeight;
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_groups.Count == 0)
            {
                return;
            }

            var rect = contentRect;
            var drawableHeight = rect.height - TopPadding - LabelRowHeight;
            if (rect.width <= 0 || drawableHeight <= 0)
            {
                return;
            }

            var maxValue = _groups
                .SelectMany(g => new[] { g.PlannedMinor, g.ActualMinor, g.CommittedMinor })
                .DefaultIfEmpty(0L)
                .Max();
            if (maxValue <= 0)
            {
                return;
            }

            var painter = context.painter2D;
            var groupWidth = rect.width / _groups.Count;
            var barsWidth = Mathf.Max(0f, groupWidth - GroupGap);
            var barWidth = Mathf.Max(0f, (barsWidth - 2 * BarGap) / 3f);

            for (var i = 0; i < _groups.Count; i++)
            {
                var group = _groups[i];
                var groupLeft = i * groupWidth + GroupGap / 2f;

                DrawBar(painter, groupLeft, barWidth, group.PlannedMinor, maxValue, drawableHeight, PlannedColor);
                DrawBar(painter, groupLeft + barWidth + BarGap, barWidth, group.ActualMinor, maxValue, drawableHeight, ActualColor);
                DrawBar(painter, groupLeft + 2 * (barWidth + BarGap), barWidth, group.CommittedMinor, maxValue, drawableHeight, CommittedColor);
            }
        }

        private static void DrawBar(
            Painter2D painter, float left, float width, long valueMinor, long maxValue, float drawableHeight, Color color)
        {
            if (width <= 0 || valueMinor <= 0)
            {
                return;
            }

            var barHeight = drawableHeight * (float)((double)valueMinor / maxValue);
            var top = TopPadding + drawableHeight - barHeight;
            var bottom = TopPadding + drawableHeight;

            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(left, top));
            painter.LineTo(new Vector2(left + width, top));
            painter.LineTo(new Vector2(left + width, bottom));
            painter.LineTo(new Vector2(left, bottom));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
