using Microsoft.AspNetCore.Components;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorApp1.Components.Shared.Select
{
	public partial class MultiSelectCombo<TItem> : ComponentBase
	{
		[Parameter] public List<TItem> Items { get; set; } = new();
		[Parameter] public List<TItem> SelectedValues { get; set; } = new();
		[Parameter] public EventCallback<List<TItem>> SelectedValuesChanged { get; set; }
		[Parameter] public string Placeholder { get; set; } = "-- Select options --";

		private bool isOpen = false;

		private void ToggleDropdown()
		{
			isOpen = !isOpen;
		}

		private async Task ToggleSelection(TItem item)
		{
			if (SelectedValues.Contains(item))
			{
				SelectedValues.Remove(item);
			}
			else
			{
				SelectedValues.Add(item);
			}

			await SelectedValuesChanged.InvokeAsync(SelectedValues);
		}
	}
}
