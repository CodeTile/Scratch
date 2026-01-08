namespace FluentUI.Tests.Reqnroll.Drivers;

using FluentUI.Components.Shared;

public class CheckBoxListDriver<TItem>
{
	public CheckBoxList<TItem> Component { get; private set; }

	public CheckBoxListDriver()
	{
		Reset();
	}

	public void Reset()
	{
		Component = new CheckBoxList<TItem>
		{
			SelectedValues = new List<string>(),
			SelectedTexts = new List<string>()
		};
	}

	public void Toggle(string value, string text, bool isChecked)
	{
		Component.ToggleValue(value, text, isChecked);
	}
}