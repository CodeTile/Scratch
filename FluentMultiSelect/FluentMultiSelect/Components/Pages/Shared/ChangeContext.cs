namespace FluentMultiSelect.Components.Pages.Shared;

public sealed class SelectChangeContext
{
	public bool IsMulti { get; init; }
	public string? Value { get; init; }
	public IReadOnlyList<string>? Values { get; init; }
}
