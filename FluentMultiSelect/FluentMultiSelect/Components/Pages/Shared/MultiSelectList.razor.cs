using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using System.Collections.Generic;

namespace FluentMultiSelect.Components.Pages.Shared;

public partial class MultiSelectList : ComponentBase
{
	[Parameter] public bool IsMultiSelect { get; set; }
	[Parameter] public string? Label { get; set; }
	[Parameter] public string? Placeholder { get; set; }
	[Parameter] public IEnumerable<string> Data { get; set; }

	// SINGLE SELECT
	[Parameter] public string Value { get; set; }
	[Parameter] public EventCallback<string> ValueChanged { get; set; }

	// MULTI SELECT
	[Parameter] public IEnumerable<string> Values { get; set; }
	[Parameter] public EventCallback<IEnumerable<string>> ValuesChanged { get; set; }

	// COMMON OnChange callback
	[Parameter] public EventCallback<object> OnChange { get; set; }

	// Extra property (Filter)
	[Parameter] public string Filter { get; set; }

	private async Task OnValueChanged(string newValue)
	{
		Value = newValue;
		await ValueChanged.InvokeAsync(newValue);
		await OnChange.InvokeAsync(newValue);
	}

	private async Task OnValuesChanged(IEnumerable<string> newValues)
	{
		Console.WriteLine("".PadRight(200, 'A'));
		Values = newValues;
		await ValuesChanged.InvokeAsync(newValues);
		await OnChange.InvokeAsync(newValues);
	}
}
