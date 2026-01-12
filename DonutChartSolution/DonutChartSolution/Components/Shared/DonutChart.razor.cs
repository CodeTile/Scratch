using Microsoft.AspNetCore.Components;

namespace DonutChartSolution.Components.Shared;

/// <summary>
/// A reusable SVG donut or pie chart component with slice interaction,
/// tooltips, and empty-state handling.
/// </summary>
public partial class DonutChart : ComponentBase
{
	/// <summary>
	/// The title displayed above the chart.
	/// </summary>
	[Parameter] public string? Title { get; set; }

	/// <summary>
	/// The inner title displayed inside the donut center.
	/// Only applies when <see cref="IsDonut"/> is true.
	/// </summary>
	[Parameter] public string? InnerTitle { get; set; }

	/// <summary>
	/// Whether the chart is rendered as a donut (true) or pie (false).
	/// </summary>
	[Parameter] public bool IsDonut { get; set; }

	/// <summary>
	/// Thickness of the donut ring.
	/// </summary>
	[Parameter] public int Thickness { get; set; } = 20;

	/// <summary>
	/// Dictionary of values used for donut mode.
	/// </summary>
	[Parameter] public Dictionary<string, int>? Data { get; set; }

	/// <summary>
	/// List of label/value pairs used for pie mode.
	/// </summary>
	[Parameter] public IEnumerable<KeyValuePair<string, int>>? Items { get; set; }

	/// <summary>
	/// Labels to include in the chart.
	/// </summary>
	[Parameter] public IEnumerable<string>? IncludeLabels { get; set; }

	/// <summary>
	/// Fired when a slice is clicked.
	/// </summary>
	[Parameter] public EventCallback<string> OnSliceClick { get; set; }

	/// <summary>
	/// Fired when the donut center is clicked.
	/// </summary>
	[Parameter] public EventCallback OnCenterClick { get; set; }

	/// <summary>
	/// Whether to display the legend below the chart.
	/// </summary>
	[Parameter] public bool ShowLegend { get; set; } = false;

	/// <summary>
	/// Computed total of all visible slice values.
	/// </summary>
	protected int TotalValue => Slices.Sum(s => s.Value);

	/// <summary>
	/// True when the chart contains no meaningful data.
	/// </summary>
	protected bool IsEmpty => TotalValue < 1;

	/// <summary>
	/// Inner radius of the donut ring.
	/// </summary>
	internal int InnerRadius => 100 - Thickness;

	/// <summary>
	/// List of computed slices.
	/// </summary>
	internal List<DonutSlice> Slices { get; private set; } = [];

	/// <summary>
	/// Whether the tooltip is visible.
	/// </summary>
	protected bool ShowTooltip { get; set; }

	/// <summary>
	/// Tooltip label text.
	/// </summary>
	protected string TooltipLabel { get; set; } = string.Empty;

	/// <summary>
	/// Tooltip value text.
	/// </summary>
	protected string TooltipValue { get; set; } = string.Empty;

	/// <summary>
	/// Rebuilds slices when parameters change.
	/// </summary>
	protected override void OnParametersSet()
	{
		BuildSlices();
	}

	/// <summary>
	/// Builds slice geometry from input data.
	/// </summary>
	private void BuildSlices()
	{
		Slices.Clear();

		var source =
			(IsDonut
				? Data?.Where(d => IncludeLabels?.Contains(d.Key) ?? true)
				: Items?.Where(i => IncludeLabels?.Contains(i.Key) ?? true))
			?? Enumerable.Empty<KeyValuePair<string, int>>();

		// Remove zero-value and filter labels
		source = source.Where(s => s.Value > 0);

		int total = source.Sum(s => s.Value);
		if (total < 1)
			return;

		double startAngle = 0;

		foreach (var item in source)
		{
			double sweep = (item.Value / (double)total) * 360.0;

			var slice = new DonutSlice(item.Key, item.Value, startAngle, sweep);
			Slices.Add(slice);

			startAngle += sweep;
		}
	}

	private async Task OnSliceClickAsync(DonutSlice slice)
	{
		await OnSliceClick.InvokeAsync(slice.Label);
	}

	private void OnSliceHover(DonutSlice slice)
	{
		TooltipLabel = slice.Label;
		TooltipValue = slice.Value.ToString("N0");
		ShowTooltip = true;
	}

	private void ClearHover()
	{
		ShowTooltip = false;
	}

	private void OnCenterHover()
	{
		TooltipLabel = InnerTitle ?? string.Empty;
		TooltipValue = TotalValue.ToString("N0");
		ShowTooltip = true;
	}

	private async Task OnCenterClickAsync()
	{
		await OnCenterClick.InvokeAsync();
	}
}

/// <summary>
/// Represents a single slice of the chart.
/// </summary>
public class DonutSlice
{
	public string Label { get; }
	public int Value { get; }
	public string PathData { get; }
	public string Color { get; }

	public DonutSlice(string label, int value, double startAngle, double sweepAngle)
	{
		Label = label;
		Value = value;
		Color = GenerateColor(label);
		PathData = BuildPath(startAngle, sweepAngle);
	}

	private static string GenerateColor(string key)
	{
		int hash = key.GetHashCode();
		var random = new Random(hash);
		return $"hsl({random.Next(0, 360)}, 70%, 55%)";
	}

	private static string BuildPath(double startAngle, double sweepAngle)
	{
		double startRad = (Math.PI / 180) * startAngle;
		double endRad = (Math.PI / 180) * (startAngle + sweepAngle);

		double radius = 90; // reduced from 100

		double x1 = 100 + radius * Math.Cos(startRad);
		double y1 = 100 + radius * Math.Sin(startRad);
		double x2 = 100 + radius * Math.Cos(endRad);
		double y2 = 100 + radius * Math.Sin(endRad);

		int largeArc = sweepAngle > 180 ? 1 : 0;

		return $"M100,100 L{x1},{y1} A{radius},{radius} 0 {largeArc} 1 {x2},{y2} Z";
	}
}