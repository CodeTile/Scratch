namespace FluentUI.Tests.Reqnroll.StepDefinitions;

using FluentUI.Tests.Reqnroll.Drivers;

using Reqnroll;

using Shouldly;

[Binding]
public class CheckBoxListStepDefinitions
{
	private readonly CheckBoxListDriver<object> _driver = new();

	[Given(@"a new CheckBoxList component")]
	public void GivenANewCheckBoxListComponent()
	{
		_driver.Reset();
	}

	[Given(@"a CheckBoxList component with value ""(.*)"" and text ""(.*)"" selected")]
	public void GivenAComponentWithPreselectedValue(string value, string text)
	{
		_driver.Component.SelectedValues.Add(value);
		_driver.Component.SelectedTexts.Add(text);
	}

	[Given(@"a CheckBoxList component with values ""(.*)"" and texts ""(.*)"" selected")]
	public void GivenMultiplePreselectedValues(string values, string texts)
	{
		var valueList = values.Split(',').Select(v => v.Trim());
		var textList = texts.Split(',').Select(t => t.Trim());

		_driver.Component.SelectedValues.AddRange(valueList);
		_driver.Component.SelectedTexts.AddRange(textList);
	}

	[When(@"I toggle value ""(.*)"" with text ""(.*)"" as checked")]
	public void WhenIToggleValueAsChecked(string value, string text)
	{
		_driver.Toggle(value, text, true);
	}

	[When(@"I toggle value ""(.*)"" with text ""(.*)"" as unchecked")]
	public void WhenIToggleValueAsUnchecked(string value, string text)
	{
		_driver.Toggle(value, text, false);
	}

	[When(@"I toggle the following values:")]
	public void WhenIToggleTheFollowingValues(Table table)
	{
		foreach (var row in table.Rows)
		{
			var value = row["value"];
			var text = row["text"];
			_driver.Toggle(value, text, true);
		}
	}

	[Then(@"the selected values should contain ""(.*)""")]
	public void ThenSelectedValuesShouldContain(string value)
	{
		_driver.Component.SelectedValues.ShouldContain(value);
	}

	[Then(@"the selected texts should contain ""(.*)""")]
	public void ThenSelectedTextsShouldContain(string text)
	{
		_driver.Component.SelectedTexts.ShouldContain(text);
	}

	[Then(@"the selected values should not contain ""(.*)""")]
	public void ThenSelectedValuesShouldNotContain(string value)
	{
		_driver.Component.SelectedValues.ShouldNotContain(value);
	}

	[Then(@"the selected texts should not contain ""(.*)""")]
	public void ThenSelectedTextsShouldNotContain(string text)
	{
		_driver.Component.SelectedTexts.ShouldNotContain(text);
	}

	[Then(@"the selected values count should be (.*)")]
	public void ThenSelectedValuesCountShouldBe(int count)
	{
		_driver.Component.SelectedValues.Count.ShouldBe(count);
	}

	[Then(@"the selected texts count should be (.*)")]
	public void ThenSelectedTextsCountShouldBe(int count)
	{
		_driver.Component.SelectedTexts.Count.ShouldBe(count);
	}

	[Then(@"the selected values should be in order ""(.*)""")]
	public void ThenSelectedValuesShouldBeInOrder(string expectedCsv)
	{
		var expected = expectedCsv.Split(',').Select(x => x.Trim()).ToList();
		_driver.Component.SelectedValues.ShouldBe(expected);
	}
}