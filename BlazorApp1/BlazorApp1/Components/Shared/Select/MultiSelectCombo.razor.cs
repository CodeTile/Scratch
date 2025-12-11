

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorApp1.Components.Shared.Select;

public partial class MultiSelectComboBase : ComponentBase
{
	[Parameter] public string Placeholder { get; set; } = "Select...";
	[Parameter] public string[] Items { get; set; } = Array.Empty<string>();
	[Parameter] public string[] SelectedValues { get; set; } = Array.Empty<string>();
	[Parameter] public EventCallback<string[]> SelectedValuesChanged { get; set; }
	[Parameter] public EventCallback<string[]> OnSelectionChanged { get; set; }

	protected bool IsOpen { get; set; } = false;

	// Toggle dropdown visibility
	protected void ToggleDropdown()
	{
		IsOpen = !IsOpen;
		StateHasChanged();
	}

	// Public method to close dropdown (for click-outside)
	public void CloseDropdown()
	{
		if (IsOpen)
		{
			IsOpen = false;
			StateHasChanged();
		}
	}

	protected async Task OnItemChanged(string item, object? isChecked)
	{
		var list = SelectedValues.ToList();

		if (isChecked is bool b)
		{
			if (b && !list.Contains(item))
				list.Add(item);
			else if (!b)
				list.Remove(item);
		}

		await UpdateSelection(list.ToArray());
	}

	protected async Task SelectAll()
	{
		await UpdateSelection(Items.ToArray());
	}

	protected async Task ClearAll()
	{
		await UpdateSelection(Array.Empty<string>());
	}

	private async Task UpdateSelection(string[] newValues)
	{
		await SelectedValuesChanged.InvokeAsync(newValues);
		if (OnSelectionChanged.HasDelegate)
			await OnSelectionChanged.InvokeAsync(newValues);
	}

	protected string DisplayText =>
		SelectedValues.Length == 0 ? Placeholder : string.Join(", ", SelectedValues);
}

