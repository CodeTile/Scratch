using Microsoft.AspNetCore.Components;

namespace FluentUI.Components.Shared
{
	/// <summary>
	/// A reusable checklist component that supports both complex objects and simple string lists.
	/// Automatically checks all items not included in <see cref="ExcludedTexts"/> and provides two-way binding.
	/// </summary>
	/// <typeparam name="TItem">The type of items in the checklist.</typeparam>
	public partial class CheckBoxList<TItem> : ComponentBase
	{
		/// <summary>
		/// Gets or sets the collection of items to display in the checklist.
		/// Can be complex objects or simple strings.
		/// </summary>
		[Parameter]
		public IEnumerable<TItem> Data { get; set; } = Enumerable.Empty<TItem>();

		/// <summary>
		/// Gets or sets a function to extract the display text from each item.
		/// Optional if <typeparamref name="TItem"/> is a <see cref="string"/>.
		/// </summary>
		[Parameter]
		public Func<TItem, string>? TextField { get; set; }

		/// <summary>
		/// Gets or sets a function to extract the value from each item.
		/// Optional if <typeparamref name="TItem"/> is a <see cref="string"/>.
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
		/// Event callback triggered when <see cref="SelectedValues"/> changes.
		/// Allows two-way binding from the parent component.
		/// </summary>
		[Parameter]
		public EventCallback<List<string>> SelectedValuesChanged { get; set; }

		/// <summary>
		/// Gets or sets the list of selected display texts.
		/// Useful for when the parent needs both ID and human-readable label.
		/// </summary>
		[Parameter]
		public List<string> SelectedTexts { get; set; } = new();

		/// <summary>
		/// Event callback triggered when <see cref="SelectedTexts"/> changes.
		/// Allows two-way binding from the parent component.
		/// </summary>
		[Parameter]
		public EventCallback<List<string>> SelectedTextsChanged { get; set; }

		/// <summary>
		/// Gets or sets the collection of display texts that should start unchecked.
		/// Matching is case-insensitive.
		/// </summary>
		[Parameter]
		public IEnumerable<string>? ExcludedTexts { get; set; }

		/// <summary>
		/// Internal cached set of excluded texts for fast, case-insensitive lookup.
		/// </summary>
		private HashSet<string>? _excludedTexts;

		/// <summary>
		/// Tracks whether the initial selection has been applied.
		/// Prevents overwriting user changes on subsequent parameter updates.
		/// </summary>
		private bool _initialized;

		/// <summary>
		/// Called by the framework when component parameters are set or updated.
		/// Initializes the selected values and texts based on <see cref="ExcludedTexts"/> only once.
		/// </summary>
		protected override async Task OnParametersSetAsync()
		{
			if (!_initialized)
			{
				// Build a case-insensitive set of excluded texts
				_excludedTexts = ExcludedTexts?
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.ToHashSet(StringComparer.OrdinalIgnoreCase);

				// Track whether SelectedValues/Texts changed during initialization
				bool valuesChanged = false;
				bool textsChanged = false;

				// Loop through all items in Data
				foreach (var item in Data)
				{
					var text = TextField != null ? TextField(item) : item?.ToString() ?? string.Empty;
					var value = ValueField != null ? ValueField(item)?.ToString() ?? text : text;

					if (_excludedTexts != null && _excludedTexts.Contains(text))
					{
						// Excluded items are unchecked
						if (SelectedValues.Remove(value)) valuesChanged = true;
						if (SelectedTexts.Remove(text)) textsChanged = true;
					}
					else
					{
						// Non-excluded items are checked
						if (!SelectedValues.Contains(value)) { SelectedValues.Add(value); valuesChanged = true; }
						if (!SelectedTexts.Contains(text)) { SelectedTexts.Add(text); textsChanged = true; }
					}
				}

				_initialized = true;

				// Notify parent component if any changes occurred
				if (valuesChanged)
					await SelectedValuesChanged.InvokeAsync(SelectedValues);

				if (textsChanged)
					await SelectedTextsChanged.InvokeAsync(SelectedTexts);
			}
		}

		/// <summary>
		/// Handles changes to checkbox state.
		/// Adds or removes the value/text from the selected lists and triggers event callbacks.
		/// </summary>
		/// <param name="value">The string value associated with the checkbox.</param>
		/// <param name="text">The display text associated with the checkbox.</param>
		/// <param name="changed">The new checked state from the checkbox input.</param>
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

			// Notify parent components
			await SelectedValuesChanged.InvokeAsync(SelectedValues);
			await SelectedTextsChanged.InvokeAsync(SelectedTexts);
		}
	}
}