using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// The cash-flow chart — a hand-drawn line via <see cref="Painter2D"/>, no third-party
    /// charting library (ADR-103). Solid segment for the real (already-happened) portion of the
    /// timeline, dashed for the forecast portion, a small marker at the lowest point.
    /// See docs/07-Interface.md §8.3. Always paired in the UXML with a textual table right below
    /// it (§8.4's "toujours une alternative textuelle à côté du graphique"). Hover shows the
    /// nearest day's exact date/balance/réel-ou-prévu in a tooltip (§8.4, ADR-124).
    /// </summary>
    public sealed class LineChartElement : VisualElement
    {
        private static readonly Color ActualLineColorLight = new(0.180f, 0.227f, 0.349f); // --color-accent
        private static readonly Color ActualLineColorDark = new(0.561f, 0.627f, 0.839f);
        private static readonly Color ForecastLineColorLight = new(0.525f, 0.549f, 0.580f); // --color-ink-400
        private static readonly Color ForecastLineColorDark = new(0.463f, 0.486f, 0.525f);
        private static readonly Color LowestMarkerColorLight = new(0.663f, 0.463f, 0.184f); // --color-gold
        private static readonly Color LowestMarkerColorDark = new(0.851f, 0.663f, 0.310f);
        private static readonly Color ZeroLineColorLight = new(0.882f, 0.874f, 0.827f); // --color-border
        private static readonly Color ZeroLineColorDark = new(0.169f, 0.184f, 0.216f);

        private const float TopPadding = 12f;
        private const float BottomPadding = 12f;
        private const float SidePadding = 6f;
        private const float LineWidth = 2f;
        private const float MarkerSize = 5f;

        private readonly Label _tooltip;
        private IReadOnlyList<ChartPointViewModel> _points = Array.Empty<ChartPointViewModel>();
        private bool _darkTheme;

        public IReadOnlyList<ChartPointViewModel> Points
        {
            get => _points;
            set
            {
                _points = value ?? Array.Empty<ChartPointViewModel>();
                MarkDirtyRepaint();
            }
        }

        /// <summary>Which color pair to draw with — set once per construction by the owning
        /// controller from the current AppSettings.Theme (ADR-135), never read from theme.uss
        /// directly since Painter2D colors are plain C# values, not CSS custom properties.</summary>
        public bool DarkTheme
        {
            get => _darkTheme;
            set
            {
                _darkTheme = value;
                MarkDirtyRepaint();
            }
        }

        private Color ActualLineColor => _darkTheme ? ActualLineColorDark : ActualLineColorLight;
        private Color ForecastLineColor => _darkTheme ? ForecastLineColorDark : ForecastLineColorLight;
        private Color LowestMarkerColor => _darkTheme ? LowestMarkerColorDark : LowestMarkerColorLight;
        private Color ZeroLineColor => _darkTheme ? ZeroLineColorDark : ZeroLineColorLight;

        public LineChartElement()
        {
            generateVisualContent += OnGenerateVisualContent;
            _tooltip = ChartTooltip.Create(this);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerLeaveEvent>(_ => ChartTooltip.Hide(_tooltip));
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            var index = FindNearestPointIndex(_points.Count, contentRect.width, SidePadding, evt.localPosition.x);
            if (index is null)
            {
                ChartTooltip.Hide(_tooltip);
                return;
            }

            var point = _points[index.Value];
            var certainty = point.IsActual ? "réel" : "prévu";
            var text = $"{DateFormat.Short(point.Date)} — {MoneyFormat.Format(point.ClosingBalanceMinor)} ({certainty})";
            ChartTooltip.Show(_tooltip, contentRect, text, evt.localPosition);
        }

        /// <summary>Which day's point is closest to a given horizontal pointer position — pure
        /// geometry, no dependency on this instance's state, so it can be unit-tested directly
        /// (batchmode cannot simulate pointer events). Mirrors <c>PointAt</c>'s own X placement in
        /// <see cref="OnGenerateVisualContent"/> exactly, just inverted. Null below two points,
        /// matching the draw guard: nothing is drawn, so nothing should be hoverable either.
        /// See docs/07-Interface.md §8.4, ADR-124.</summary>
        public static int? FindNearestPointIndex(int pointCount, float elementWidth, float sidePadding, float localX)
        {
            if (pointCount < 2)
            {
                return null;
            }

            var drawableWidth = elementWidth - 2 * sidePadding;
            if (drawableWidth <= 0)
            {
                return null;
            }

            var t = Mathf.Clamp01((localX - sidePadding) / drawableWidth);
            return Mathf.RoundToInt(t * (pointCount - 1));
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (_points.Count < 2)
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

            var minBalance = _points.Min(p => p.ClosingBalanceMinor);
            var maxBalance = _points.Max(p => p.ClosingBalanceMinor);
            if (minBalance == maxBalance)
            {
                // A perfectly flat line still needs a non-zero range to divide by.
                minBalance -= 100;
                maxBalance += 100;
            }

            var valuePadding = (maxBalance - minBalance) * 0.1;
            var paddedMin = minBalance - valuePadding;
            var paddedMax = maxBalance + valuePadding;
            var paddedRange = paddedMax - paddedMin;

            Vector2 PointAt(int index)
            {
                var x = SidePadding + (_points.Count == 1 ? 0f : (float)index / (_points.Count - 1) * drawableWidth);
                var normalized = (_points[index].ClosingBalanceMinor - paddedMin) / paddedRange;
                var y = TopPadding + drawableHeight - (float)normalized * drawableHeight;
                return new Vector2(x, y);
            }

            var painter = context.painter2D;

            if (paddedMin < 0 && paddedMax > 0)
            {
                var zeroNormalized = (0 - paddedMin) / paddedRange;
                var zeroY = TopPadding + drawableHeight - (float)zeroNormalized * drawableHeight;
                painter.strokeColor = ZeroLineColor;
                painter.lineWidth = 1f;
                DrawDashedLine(painter, new Vector2(SidePadding, zeroY), new Vector2(SidePadding + drawableWidth, zeroY), 4f, 4f);
            }

            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            for (var i = 0; i < _points.Count - 1; i++)
            {
                var from = PointAt(i);
                var to = PointAt(i + 1);
                var isActualSegment = _points[i].IsActual && _points[i + 1].IsActual;
                painter.strokeColor = isActualSegment ? ActualLineColor : ForecastLineColor;

                if (isActualSegment)
                {
                    painter.BeginPath();
                    painter.MoveTo(from);
                    painter.LineTo(to);
                    painter.Stroke();
                }
                else
                {
                    DrawDashedLine(painter, from, to, 6f, 4f);
                }
            }

            var lowestIndex = 0;
            for (var i = 1; i < _points.Count; i++)
            {
                if (_points[i].ClosingBalanceMinor < _points[lowestIndex].ClosingBalanceMinor)
                {
                    lowestIndex = i;
                }
            }

            var lowestPos = PointAt(lowestIndex);
            painter.fillColor = LowestMarkerColor;
            painter.BeginPath();
            painter.MoveTo(lowestPos + new Vector2(0, -MarkerSize));
            painter.LineTo(lowestPos + new Vector2(MarkerSize, 0));
            painter.LineTo(lowestPos + new Vector2(0, MarkerSize));
            painter.LineTo(lowestPos + new Vector2(-MarkerSize, 0));
            painter.ClosePath();
            painter.Fill();
        }

        /// <summary>Painter2D has no line-dash property (unlike HTML Canvas) — walked by hand as
        /// alternating drawn/skipped sub-segments within one path/stroke.</summary>
        private static void DrawDashedLine(Painter2D painter, Vector2 from, Vector2 to, float dashLength, float gapLength)
        {
            var direction = to - from;
            var totalLength = direction.magnitude;
            if (totalLength <= 0f)
            {
                return;
            }

            var normalizedDirection = direction / totalLength;
            var distance = 0f;
            var drawing = true;

            painter.BeginPath();
            while (distance < totalLength)
            {
                var segmentLength = Mathf.Min(drawing ? dashLength : gapLength, totalLength - distance);
                if (drawing)
                {
                    painter.MoveTo(from + normalizedDirection * distance);
                    painter.LineTo(from + normalizedDirection * (distance + segmentLength));
                }

                distance += segmentLength;
                drawing = !drawing;
            }

            painter.Stroke();
        }
    }
}
