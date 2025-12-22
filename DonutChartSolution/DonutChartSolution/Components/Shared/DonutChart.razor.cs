using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

using MudBlazor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DonutChartSolution.Components.Shared
{
	public partial class DonutChart : ComponentBase
	{
		private MudChart? _chartRef;

		[Inject] public NavigationManager NavigationManager { get; set; } = default!;
		[Inject] public IJSRuntime JS { get; set; } = default!;

		// Unique ID for the wrapper div
		protected string ChartContainerId { get; } = "donut-chart-" + Guid.NewGuid().ToString("N");

		private IEnumerable<KeyValuePair<string, int>> _data = Enumerable.Empty<KeyValuePair<string, int>>();

		[Parameter]
		public IEnumerable<KeyValuePair<string, int>>? Data
		{
			get => _data;
			set => _data = value ?? Enumerable.Empty<KeyValuePair<string, int>>();
		}

		[Parameter]
		public Dictionary<string, int>? DataDictionary
		{
			get => _data.ToDictionary(k => k.Key, v => v.Value);
			set
			{
				if (value != null)
					_data = value;
			}
		}

		[Parameter] public string Title { get; set; } = "Donut Chart";
		[Parameter] public string InnerLabel { get; set; } = "Total";
		[Parameter] public string CenterTooltipText { get; set; } = "Click to view weather";
		[Parameter] public string Width { get; set; } = "400px";
		[Parameter] public string Height { get; set; } = "400px";

		public double[] DataValues => _data.Select(kv => (double)kv.Value).ToArray();
		public string[] DataLabels => _data.Select(kv => kv.Key).ToArray();
		public int TotalValue => _data.Sum(kv => kv.Value);

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await JS.InvokeVoidAsync("donutChart.registerSliceClicks", ChartContainerId);
			}
		}

		private void OnCenterClick(MouseEventArgs _)
		{
			NavigationManager.NavigateTo("/weather");
		}

		[JSInvokable]
		public void SliceClicked(int index)
		{
			NavigationManager.NavigateTo("/counter");
		}
	}
}
