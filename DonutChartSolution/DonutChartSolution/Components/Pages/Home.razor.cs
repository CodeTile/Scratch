using Microsoft.AspNetCore.Components;

namespace DonutChartSolution.Components.Pages;

public partial class Home : ComponentBase, IDisposable
{
	[Inject] private NavigationManager Nav { get; set; } = default!;

	// -----------------------------
	// Donut chart data
	// -----------------------------
	private Dictionary<string, int> SalesData = new()
	{
		["North"] = 120,
		["South"] = 80,
		["East"] = 140,
		["West"] = 60
	};

	private List<string> AllSalesLabels => SalesData.Keys.ToList();
	private IEnumerable<string>? CurrentSalesLabels;

	// -----------------------------
	// Pie chart data
	// -----------------------------
	private IEnumerable<KeyValuePair<string, int>> TaskItems =
		new List<KeyValuePair<string, int>>
		{
			new("Completed", 30),
			new("In Progress", 15),
			new("Blocked", 5)
		};

	private List<string> AllTaskLabels => TaskItems.Select(t => t.Key).ToList();
	private IEnumerable<string>? CurrentTaskLabels;

	// -----------------------------
	// Timers (fully qualified)
	// -----------------------------
	private System.Timers.Timer? _salesTimer;

	private System.Timers.Timer? _taskTimer;

	private readonly Random _rand = new();

	protected override void OnInitialized()
	{
		CurrentSalesLabels = AllSalesLabels;
		CurrentTaskLabels = AllTaskLabels;

		_salesTimer = new System.Timers.Timer(3000);
		_salesTimer.Elapsed += (_, __) => UpdateSalesLabels();
		_salesTimer.AutoReset = true;
		_salesTimer.Enabled = true;

		_taskTimer = new System.Timers.Timer(3000);
		_taskTimer.Elapsed += (_, __) => UpdateTaskLabels();
		_taskTimer.AutoReset = true;
		_taskTimer.Enabled = true;
	}

	private void UpdateSalesLabels()
	{
		var count = _rand.Next(1, AllSalesLabels.Count + 1);
		CurrentSalesLabels = AllSalesLabels
			.OrderBy(_ => _rand.Next())
			.Take(count)
			.ToList();

		InvokeAsync(StateHasChanged);
	}

	private void UpdateTaskLabels()
	{
		var count = _rand.Next(1, AllTaskLabels.Count + 1);
		CurrentTaskLabels = AllTaskLabels
			.OrderBy(_ => _rand.Next())
			.Take(count)
			.ToList();

		InvokeAsync(StateHasChanged);
	}

	private void HandleSliceClick(string label)
	{
		Console.WriteLine($"[Home] Slice clicked: {label}");
		Nav.NavigateTo("/counter");
	}

	private void HandleCenterClick()
	{
		Console.WriteLine("[Home] Center clicked");
		Nav.NavigateTo("/weather");
	}

	public void Dispose()
	{
		_salesTimer?.Dispose();
		_taskTimer?.Dispose();
	}
}