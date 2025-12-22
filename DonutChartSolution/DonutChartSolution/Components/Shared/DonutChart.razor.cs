using Microsoft.AspNetCore.Components;

namespace DonutChartSolution.Components.Shared
{
	/// <summary>
	/// A reusable SVG-based donut or pie chart component that supports
	/// slice selection, center selection, tooltips, and dynamic data binding.
	/// </summary>
	public partial class DonutChart : ComponentBase
	{
		/// <summary>
		/// Internal representation of a computed chart slice, including geometry,
		/// color, label, and SVG path data.
		/// </summary>
		private class SliceInfo
		{
			/// <summary>
			/// The label associated with the slice (e.g., region name).
			/// </summary>
			public string Label { get; set; } = string.Empty;

			/// <summary>
			/// The numeric value represented by the slice.
			/// </summary>
			public int Value { get; set; }

			/// <summary>
			/// The starting angle of the slice in degrees.
			/// </summary>
			public double StartAngle { get; set; }

			/// <summary>
			/// The sweep angle of the slice in degrees.
			/// </summary>
			public double SweepAngle { get; set; }

			/// <summary>
			/// The fill color used to render the slice.
			/// </summary>
			public string Color { get; set; } = "#cccccc";

			/// <summary>
			/// The computed SVG path string used to draw the slice.
			/// </summary>
			public string PathData { get; set; } = string.Empty;
		}

		/// <summary>
		/// The list of computed slices used to render the chart.
		/// </summary>
		private List<SliceInfo> Slices = new();

		/// <summary>
		/// Indicates whether the tooltip is currently visible.
		/// </summary>
		private bool ShowTooltip { get; set; }

		/// <summary>
		/// The label displayed inside the tooltip.
		/// </summary>
		private string TooltipLabel { get; set; } = string.Empty;

		/// <summary>
		/// The numeric value displayed inside the tooltip.
		/// </summary>
		private string TooltipValue { get; set; } = string.Empty;

		/// <summary>
		/// The outer radius of the donut or pie chart.
		/// </summary>
		private double OuterRadius => 90;

		/// <summary>
		/// The thickness of the donut ring. Ignored when <see cref="IsDonut"/> is false.
		/// </summary>
		[Parameter] public double? Thickness { get; set; }

		/// <summary>
		/// The computed inner radius of the donut. Zero for pie charts.
		/// </summary>
		private double InnerRadius =>
			IsDonut ? OuterRadius - (Thickness ?? 40) : 0;

		/// <summary>
		/// The total of all slice values.
		/// </summary>
		private int TotalValue => Slices.Sum(s => s.Value);

		/// <summary>
		/// Data supplied as an enumerable of key/value pairs.
		/// </summary>
		[Parameter] public IEnumerable<KeyValuePair<string, int>>? Items { get; set; }

		/// <summary>
		/// Data supplied as a dictionary of labels and values.
		/// </summary>
		[Parameter] public Dictionary<string, int>? Data { get; set; }

		/// <summary>
		/// Determines whether the chart is rendered as a donut (true) or a pie (false).
		/// </summary>
		[Parameter] public bool IsDonut { get; set; } = true;

		/// <summary>
		/// The title displayed above the chart.
		/// </summary>
		[Parameter] public string? Title { get; set; }

		/// <summary>
		/// The title displayed inside the donut center.
		/// </summary>
		[Parameter] public string? InnerTitle { get; set; }

		/// <summary>
		/// The width of the SVG element.
		/// </summary>
		[Parameter] public string Width { get; set; } = "300";

		/// <summary>
		/// The height of the SVG element.
		/// </summary>
		[Parameter] public string Height { get; set; } = "300";

		/// <summary>
		/// Event raised when a slice is clicked. Provides the slice label.
		/// </summary>
		[Parameter] public EventCallback<string> OnSliceSelected { get; set; }

		/// <summary>
		/// Event raised when the donut center is clicked.
		/// </summary>
		[Parameter] public EventCallback OnCenterSelected { get; set; }

		/// <summary>
		/// Default color palette used when rendering slices.
		/// </summary>
		private static readonly string[] DefaultColors =
		{
			"#4e79a7", "#f28e2b", "#e15759", "#76b7b2",
			"#59a14f", "#edc948", "#b07aa1", "#ff9da7",
			"#9c755f", "#bab0ab"
		};

		/// <summary>
		/// Called by the framework when parameters are set or updated.
		/// Rebuilds the slice geometry based on the supplied data.
		/// </summary>
		protected override void OnParametersSet()
		{
			var data = NormalizeData();
			BuildSlices(data);
		}

		/// <summary>
		/// Normalizes the input data so the component can accept either
		/// a dictionary or an enumerable of key/value pairs.
		/// </summary>
		private IEnumerable<KeyValuePair<string, int>> NormalizeData()
		{
			if (Data is not null) return Data;
			if (Items is not null) return Items;
			return Enumerable.Empty<KeyValuePair<string, int>>();
		}

		/// <summary>
		/// Converts the input data into a list of computed slice objects,
		/// including angles, colors, and SVG path geometry.
		/// </summary>
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

		/// <summary>
		/// Builds the SVG path string for a donut or pie slice based on
		/// start angle, sweep angle, and inner/outer radii.
		/// </summary>
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

		/// <summary>
		/// Converts degrees to radians.
		/// </summary>
		private static double DegreesToRadians(double degrees)
			=> degrees * Math.PI / 180.0;

		/// <summary>
		/// Handles slice click events and raises the <see cref="OnSliceSelected"/> callback.
		/// </summary>
		private async Task OnSliceClickAsync(SliceInfo slice)
		{
			Console.WriteLine($"[DonutChart] Slice clicked: {slice.Label}");
			if (OnSliceSelected.HasDelegate)
				await OnSliceSelected.InvokeAsync(slice.Label);
		}

		/// <summary>
		/// Handles donut center click events and raises the <see cref="OnCenterSelected"/> callback.
		/// </summary>
		private async Task OnCenterClickAsync()
		{
			Console.WriteLine("[DonutChart] Center clicked");
			if (IsDonut && OnCenterSelected.HasDelegate)
				await OnCenterSelected.InvokeAsync();
		}

		/// <summary>
		/// Displays tooltip information for the hovered slice.
		/// </summary>
		private void OnSliceHover(SliceInfo slice)
		{
			TooltipLabel = slice.Label;
			TooltipValue = slice.Value.ToString("N0");
			ShowTooltip = true;
		}

		/// <summary>
		/// Displays tooltip information for the donut center.
		/// </summary>
		private void OnCenterHover()
		{
			if (!IsDonut) return;

			TooltipLabel = InnerTitle ?? "Total";
			TooltipValue = TotalValue.ToString("N0");
			ShowTooltip = true;
		}

		/// <summary>
		/// Hides the tooltip when the mouse leaves the chart.
		/// </summary>
		private void ClearHover()
		{
			ShowTooltip = false;
		}
	}
}
