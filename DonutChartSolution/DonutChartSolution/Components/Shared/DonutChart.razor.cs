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
		/// Index of clicked slice
		/// </summary>
		public int SelectedIndex { get; set; } = -1;

		/// <summary>
		/// Values for MudChart
		/// </summary>
		public double[] DataValues => Data.Select(kv => (double)kv.Value).ToArray();

		/// <summary>
		/// Labels for MudChart
		/// </summary>
		public string[] DataLabels => Data.Select(kv => kv.Key).ToArray();

		/// <summary>
		/// Sum of all values for the center label
		/// </summary>
		public int TotalValue => Data.Sum(kv => kv.Value);

		/// <summary>
		/// Navigate to /weather when the center is clicked
		/// </summary>
		public void OnCenterClick()
		{
			Navigation.NavigateTo("/weather");
		}

		/// <summary>
		/// Called on parameter update; navigate to /counter if a slice was clicked
		/// </summary>
		protected override void OnParametersSet()
		{
			if (SelectedIndex >= 0)
			{
				Navigation.NavigateTo("/counter");
				SelectedIndex = -1; // Reset for future clicks
			}
		}
	}
}
