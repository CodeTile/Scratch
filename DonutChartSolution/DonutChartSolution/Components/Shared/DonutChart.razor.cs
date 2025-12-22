using Microsoft.AspNetCore.Components;

using System;
using System.Collections.Generic;
using System.Linq;

namespace DonutChartSolution.Components.Shared
{
	public partial class DonutChart : ComponentBase
	{
		private class SliceInfo
		{
			public string Label { get; set; } = string.Empty;
			public int Value { get; set; }
			public double StartAngle { get; set; }
			public double SweepAngle { get; set; }
			public string Color { get; set; } = "#cccccc";
			public string PathData { get; set; } = string.Empty;
		}

		private List<SliceInfo> Slices = new();

		private bool ShowTooltip { get; set; }
		private string TooltipLabel { get; set; } = string.Empty;
		private string TooltipValue { get; set; } = string.Empty;

		private double OuterRadius => 90;

		[Parameter] public double? Thickness { get; set; }

		private double InnerRadius =>
			IsDonut ? OuterRadius - (Thickness ?? 40) : 0;

		private int TotalValue => Slices.Sum(s => s.Value);

		[Parameter] public IEnumerable<KeyValuePair<string, int>>? Items { get; set; }
		[Parameter] public Dictionary<string, int>? Data { get; set; }

		[Parameter] public bool IsDonut { get; set; } = true;

		[Parameter] public string? Title { get; set; }
		[Parameter] public string? InnerTitle { get; set; }

		[Parameter] public string Width { get; set; } = "300";
		[Parameter] public string Height { get; set; } = "300";

		private static readonly string[] DefaultColors =
		{
			"#4e79a7", "#f28e2b", "#e15759", "#76b7b2",
			"#59a14f", "#edc948", "#b07aa1", "#ff9da7",
			"#9c755f", "#bab0ab"
		};

		protected override void OnParametersSet()
		{
			var data = NormalizeData();
			BuildSlices(data);
		}

		private IEnumerable<KeyValuePair<string, int>> NormalizeData()
		{
			if (Data is not null) return Data;
			if (Items is not null) return Items;
			return Enumerable.Empty<KeyValuePair<string, int>>();
		}

		private void BuildSlices(IEnumerable<KeyValuePair<string, int>> data)
		{
			var list = data.Where(kv => kv.Value > 0).ToList();
			var total = list.Sum(kv => kv.Value);

			Slices.Clear();
			if (total <= 0) return;

			double currentAngle = -90;

			for (int i = 0; i < list.Count; i++)
			{
				var kv = list[i];
				double sweep = (kv.Value / (double)total) * 360.0;

				var slice = new SliceInfo
				{
					Label = kv.Key,
					Value = kv.Value,
					StartAngle = currentAngle,
					SweepAngle = sweep,
					Color = DefaultColors[i % DefaultColors.Length],
					PathData = BuildDonutSlicePath(currentAngle, sweep)
				};

				Slices.Add(slice);
				currentAngle += sweep;
			}
		}

		private string BuildDonutSlicePath(double startAngle, double sweepAngle)
		{
			double startRad = DegreesToRadians(startAngle);
			double endRad = DegreesToRadians(startAngle + sweepAngle);

			double x0Outer = 100 + OuterRadius * Math.Cos(startRad);
			double y0Outer = 100 + OuterRadius * Math.Sin(startRad);

			double x1Outer = 100 + OuterRadius * Math.Cos(endRad);
			double y1Outer = 100 + OuterRadius * Math.Sin(endRad);

			bool largeArc = sweepAngle > 180;

			if (!IsDonut || InnerRadius <= 0)
			{
				return $"M 100 100 L {x0Outer:F3} {y0Outer:F3} " +
					   $"A {OuterRadius} {OuterRadius} 0 {(largeArc ? 1 : 0)} 1 {x1Outer:F3} {y1Outer:F3} Z";
			}

			double x0Inner = 100 + InnerRadius * Math.Cos(endRad);
			double y0Inner = 100 + InnerRadius * Math.Sin(endRad);

			double x1Inner = 100 + InnerRadius * Math.Cos(startRad);
			double y1Inner = 100 + InnerRadius * Math.Sin(startRad);

			return
				$"M {x0Outer:F3} {y0Outer:F3} " +
				$"A {OuterRadius} {OuterRadius} 0 {(largeArc ? 1 : 0)} 1 {x1Outer:F3} {y1Outer:F3} " +
				$"L {x0Inner:F3} {y0Inner:F3} " +
				$"A {InnerRadius} {InnerRadius} 0 {(largeArc ? 1 : 0)} 0 {x1Inner:F3} {y1Inner:F3} Z";
		}

		private static double DegreesToRadians(double degrees)
			=> degrees * Math.PI / 180.0;

		private void OnSliceClick(SliceInfo slice)
		{
			Navigation.NavigateTo("/counter");
		}

		private void OnCenterClick()
		{
			if (IsDonut)
				Navigation.NavigateTo("/weather");
		}

		private void OnSliceHover(SliceInfo slice)
		{
			TooltipLabel = slice.Label;
			TooltipValue = slice.Value.ToString("N0");
			ShowTooltip = true;
		}

		private void OnCenterHover()
		{
			if (!IsDonut) return;

			TooltipLabel = InnerTitle ?? "Total";
			TooltipValue = TotalValue.ToString("N0");
			ShowTooltip = true;
		}

		private void ClearHover()
		{
			ShowTooltip = false;
		}
	}
}
