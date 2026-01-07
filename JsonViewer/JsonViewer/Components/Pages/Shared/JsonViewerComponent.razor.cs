using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.FluentUI.AspNetCore.Components;        
using System.Text.Json;

namespace JsonViewer.Components.Pages.Shared
{
	public class JsonViewerComponentBase : ComponentBase
	{
		[Inject] protected IJSRuntime JS { get; set; } = default!;

		[Parameter] public object? Data { get; set; }
		[Parameter] public string? Json { get; set; }

		protected bool ShowFormatted { get; set; } = true;
		protected string SearchText { get; set; } = "";

		protected string RawJson { get; set; } = "";
		protected string FormattedJson { get; set; } = "";
		protected string DisplayJson { get; set; } = "";

		protected override void OnParametersSet()
		{
			if (Data is not null)
			{
				RawJson = JsonSerializer.Serialize(Data);

				FormattedJson = JsonSerializer.Serialize(Data, new JsonSerializerOptions
				{
					WriteIndented = true
				});
			}
			else if (!string.IsNullOrWhiteSpace(Json))
			{
				RawJson = Json!;

				try
				{
					var parsed = JsonSerializer.Deserialize<object>(Json!);
					FormattedJson = JsonSerializer.Serialize(parsed, new JsonSerializerOptions
					{
						WriteIndented = true
					});
				}
				catch
				{
					FormattedJson = RawJson;
				}
			}

			DisplayJson = ShowFormatted ? FormattedJson : RawJson;
		}

		protected void ToggleFormat()
		{
			ShowFormatted = !ShowFormatted;
			DisplayJson = ShowFormatted ? FormattedJson : RawJson;
		}

		protected async Task CopyToClipboard()
		{
			await JS.InvokeVoidAsync("navigator.clipboard.writeText", DisplayJson);
		}
	}
}
