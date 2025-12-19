using Microsoft.AspNetCore.Components;

using MudBlazor;

using System.Collections.Generic;
using System.Linq;

namespace DonutChartSolution.Components.Shared
{
	public partial class DonutChart : ComponentBase
	{
		[Parameter] public string Title { get; set; } = "Donut Chart";
		[Parameter] public string InnerLabel { get; set; } = "Total";
		[Parameter] public IEnumerable<KeyValuePair<string, int>> Data { get; set; } = new List<KeyValuePair<string, int>>();
		[Parameter] public string Width { get; set; } = "300px";
		[Parameter] public string Height { get; set; } = "300px";

		[Inject] NavigationManager Navigation { get; set; }

		/// <summary>
		/// Index of the clicked slice
		/// </summary>
		public int SelectedIndex { get; set; } = -1;

		/// <summary>
		/// Values for the chart slices
		/// </summary>
		public double[] DataValues => Data.Select(kv => (double)kv.Value).ToArray();

		/// <summary>
		/// Labels for the chart slices
		/// </summary>
		public string[] DataLabels => Data.Select(kv => kv.Key).ToArray();

		/// <summary>
		/// Called when parameters are set; used to navigate on slice click
		/// </summary>
		protected override void OnParametersSet()
		{
			if (SelectedIndex >= 0)
			{
				// Navigate to /counter when a slice is clicked
				Navigation.NavigateTo("/counter");

				// Reset selection so repeated clicks work
				SelectedIndex = -1;
			}
		}

		/// <summary>
		/// Called when the user clicks the center text
		/// </summary>
		public void OnCenterClick()
		{
			Navigation.NavigateTo("/weather");
		}

		/// <summary>
		/// Returns the sum of all values (for inner label display)
		/// </summary>
		public int TotalValue => Data.Sum(kv => kv.Value);
	}
}
