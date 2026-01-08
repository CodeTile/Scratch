using Microsoft.AspNetCore.Components;

namespace FluentUI.Components.Shared
{
	/// <summary>
	/// A reusable checklist component that supports both complex objects and simple string lists.
	/// Provides automatic tracking of selected values and selected display text.
	/// </summary>
	/// <typeparam name="TItem">
	/// The type of the items in the data source. Can be a complex object or a simple string.
	/// </typeparam>
	public partial class CheckBoxList<TItem> : ComponentBase
	{
		/// <summary>
		/// Gets or sets the collection of items to display in the checklist.
		/// Supports both complex objects and simple string lists.
		/// </summary>
		[Parameter]
		public IEnumerable<TItem> Data { get; set; } = Enumerable.Empty<TItem>();

		/// <summary>
		/// Gets or sets the function used to extract the display text from each item.
		/// Optional when <typeparamref name="TItem"/> is <see cref="string"/>.
		/// </summary>
		[Parameter]
		public Func<TItem, string>? TextField { get; set; }

		/// <summary>
		/// Gets or sets the function used to extract the value from each item.
		/// Optional when <typeparamref name="TItem"/> is <see cref="string"/>.
		/// The value is converted to a string internally.
		/// </summary>
		[Parameter]
		public Func<TItem, object>? ValueField { get; set; }

		/// <summary>
		/// Gets or sets the list of selected values.
		/// Values are always stored as strings.
		/// </summary>
		[Parameter]
		public List<string> SelectedValues { get; set; } = new();

		/// <summary>
		/// Event callback triggered whenever the <see cref="SelectedValues"/> collection changes.
		/// Allows two-way binding from the parent component.
		/// </summary>
		[Parameter]
		public EventCallback<List<string>> SelectedValuesChanged { get; set; }

		/// <summary>
		/// Gets or sets the list of selected display text values.
		/// Useful when the parent needs both the ID and the human-readable label.
		/// </summary>
		[Parameter]
		public List<string> SelectedTexts { get; set; } = new();

		/// <summary>
		/// Event callback triggered whenever the <see cref="SelectedTexts"/> collection changes.
		/// Allows two-way binding from the parent component.
		/// </summary>
		[Parameter]
		public EventCallback<List<string>> SelectedTextsChanged { get; set; }

		/// <summary>
		/// Handles checkbox state changes by adding or removing the associated value and text.
		/// Automatically triggers the appropriate event callbacks to notify the parent component.
		/// </summary>
		/// <param name="value">The value associated with the checkbox.</param>
		/// <param name="text">The display text associated with the checkbox.</param>
		/// <param name="changed">The new checked state provided by the event.</param>
		public async void ToggleValue(string value, string text, object changed)
		{
			bool isChecked = changed is bool b && b;

			if (isChecked)
			{
				if (!SelectedValues.Contains(value))
					SelectedValues.Add(value);

				if (!SelectedTexts.Contains(text))
					SelectedTexts.Add(text);
			}
			else
			{
				SelectedValues.Remove(value);
				SelectedTexts.Remove(text);
			}

			await SelectedValuesChanged.InvokeAsync(SelectedValues);
			await SelectedTextsChanged.InvokeAsync(SelectedTexts);
		}
	}
}