namespace FluentMultiSelect.Components.Pages
{
	public partial class Home
	{
		// Sample data for single select
		public List<string> Categories { get; set; } = new()
	{
		"Technology",
		"Finance",
		"Health",
		"Education",
		"Sports"
	};

		// Sample data for multi select
		public List<string> Tags { get; set; } = new()
	{
		"Urgent",
		"Important",
		"Optional",
		"Archived",
		"New"
	};

		// Bound values
		public string SelectedCategory { get; set; } = string.Empty;
		public IEnumerable<string> SelectedTags { get; set; } = new List<string>();

		// OnChange handlers
		private Task HandleCategoryChanged(object value)
		{
			Console.WriteLine($"Category changed: {value}");
			return Task.CompletedTask;
		}

		private Task HandleTagsChanged(object value)
		{
			if (value is IEnumerable<string> list)
			{
				Console.WriteLine($"Tags changed: {string.Join(", ", list)}");
			}
			return Task.CompletedTask;
		}

	}
}
