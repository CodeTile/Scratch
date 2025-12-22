using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using MudBlazor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DonutChartSolution.Components.Shared
{
	/// <summary>
	/// A reusable donut chart component that supports:
	/// - MudBlazor donut rendering
	/// - Clickable center navigation
	/// - JS-enabled slice click navigation
	/// </summary>
	public partial class DonutChart : ComponentBase, IAsyncDisposable
	{
		private IJSObjectReference? _jsModule;
		private MudChart? _chart;

		/// <summary>
		/// The title displayed above the chart and inside the donut center.
		/// </summary>
		[Parameter] public string Title { get; set; } = string.Empty;

		/// <summary>
		/// The primary data source for the donut chart.
		/// </summary>
		[Parameter] public IEnumerable<KeyValuePair<string, int>>? Data { get; set; }

		/// <summary>
		/// Optional dictionary-based data source.
		/// </summary>
		[Parameter] public Dictionary<string, int>? DataDictionary { get; set; }

		/// <summary>
		/// Optional label displayed inside the donut center.
		/// Defaults to the chart title if not provided.
		/// </summary>
		[Parameter] public string? InnerLabel { get; set; }

		/// <summary>
		/// Optional inline CSS applied directly to the chart container.
		/// Example: "width:350px;height:350px;margin:20px;"
		/// </summary>
		[Parameter] public string? Style { get; set; }

		[Inject] private NavigationManager NavigationManager { get; set; } = default!;
		[Inject] private IJSRuntime JS { get; set; } = default!;

		/// <summary>
		/// Unique DOM ID used to attach JS event handlers.
		/// </summary>
		public string ChartContainerId { get; } = $"donut-chart-{Guid.NewGuid():N}";

		/// <summary>
		/// Returns the merged data source as a double array for MudBlazor.
		/// </summary>
		public double[] DataValues =>
			(DataDictionary ?? Data ?? Enumerable.Empty<KeyValuePair<string, int>>())
			.Select(kv => (double)kv.Value)
			.ToArray();

		/// <summary>
		/// Returns the labels for the donut chart.
		/// </summary>
		public string[] DataLabels =>
			(DataDictionary ?? Data ?? Enumerable.Empty<KeyValuePair<string, int>>())
			.Select(kv => kv.Key)
			.ToArray();

		/// <summary>
		/// The total of all values, displayed in the donut center.
		/// </summary>
		public int Total => DataValues.Sum(v => (int)v);

		/// <summary>
		/// Handles the center click and navigates to the weather page.
		/// </summary>
		private void OnCenterClick()
		{
			NavigationManager.NavigateTo("/weather");
		}

		/// <summary>
		/// JS-invokable method called when a donut slice is clicked.
		/// </summary>
		/// <param name="index">The index of the clicked slice.</param>
		[JSInvokable]
		public void SliceClicked(int _)
		{
			NavigationManager.NavigateTo("/counter");
		}

		/// <summary>
		/// Loads the JS module and registers slice click handlers on first render.
		/// </summary>
		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				_jsModule = await JS.InvokeAsync<IJSObjectReference>(
					"import", "/donutchart.js");

				await _jsModule.InvokeVoidAsync(
					"registerSliceClicks", ChartContainerId);
			}
		}

		/// <summary>
		/// Disposes the JS module when the component is removed.
		/// </summary>
		public async ValueTask DisposeAsync()
		{
			if (_jsModule is not null)
			{
				await _jsModule.DisposeAsync();
			}
		}
	}
}
