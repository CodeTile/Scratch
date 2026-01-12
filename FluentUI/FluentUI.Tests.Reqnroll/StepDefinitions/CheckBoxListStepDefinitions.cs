namespace FluentUI.Components.Tests.ReqNroll
{
	[Feature("CheckBoxList selection behavior")]
	public class CheckBoxListSteps
	{
		private TestableCheckBoxList<string> _component;

		[Background]
		public void Background()
		{
			_component = new TestableCheckBoxList<string>();
			_component.OnParametersSetPublic();
		}

		[Given(@"a CheckBoxList component with value ""(.*)"" and text ""(.*)"" selected")]
		public Task GivenSingleSelected(string value, string text)
		{
			if (value == "<null>") value = null;
			if (text == "<null>") text = null;

			_component.SelectedValues.Add(value);
			_component.SelectedTexts.Add(text);
			return Task.CompletedTask;
		}

		[Given(@"a CheckBoxList component with values ""(.*)"" and texts ""(.*)"" selected")]
		public Task GivenMultipleSelected(string valuesCsv, string textsCsv)
		{
			var values = valuesCsv.Split(',').Select(v => v.Trim());
			var texts = textsCsv.Split(',').Select(t => t.Trim());

			_component.SelectedValues.AddRange(values);
			_component.SelectedTexts.AddRange(texts);
			return Task.CompletedTask;
		}

		[When(@"I toggle value ""(.*)"" with text ""(.*)"" as (checked|unchecked)")]
		public async Task WhenToggleValueAsync(string value, string text, string state)
		{
			if (value == "<null>") value = null;
			if (text == "<null>") text = null;

			bool isChecked = state == "checked";
			await Task.Run(() => _component.ToggleValue(value, text, isChecked));
		}

		[When(@"I toggle the following values:")]
		public async Task WhenToggleTableAsync(ReqNroll.DataTable table)
		{
			foreach (var row in table.Rows)
			{
				var value = row["value"];
				var text = row["text"];
				await Task.Run(() => _component.ToggleValue(value, text, true));
			}
		}

		[Then(@"the selected values should contain ""(.*)""")]
		public void ThenSelectedValuesContain(string value)
		{
			if (value == "<null>") value = null;
			CollectionAssert.Contains(_component.SelectedValues, value);
		}

		[Then(@"the selected values should not contain ""(.*)""")]
		public void ThenSelectedValuesNotContain(string value)
		{
			if (value == "<null>") value = null;
			CollectionAssert.DoesNotContain(_component.SelectedValues, value);
		}

		[Then(@"the selected texts should contain ""(.*)""")]
		public void ThenSelectedTextsContain(string text)
		{
			if (text == "<null>") text = null;
			CollectionAssert.Contains(_component.SelectedTexts, text);
		}

		[Then(@"the selected texts should not contain ""(.*)""")]
		public void ThenSelectedTextsNotContain(string text)
		{
			if (text == "<null>") text = null;
			CollectionAssert.DoesNotContain(_component.SelectedTexts, text);
		}

		[Then(@"the selected values count should be (.*)")]
		public void ThenSelectedValuesCount(int count)
		{
			Assert.AreEqual(count, _component.SelectedValues.Count);
		}

		[Then(@"the selected texts count should be (.*)")]
		public void ThenSelectedTextsCount(int count)
		{
			Assert.AreEqual(count, _component.SelectedTexts.Count);
		}

		[Then(@"the selected values should be in order ""(.*)""")]
		public void ThenSelectedValuesOrder(string csv)
		{
			var expected = csv.Split(',').Select(v => v.Trim()).ToList();
			CollectionAssert.AreEqual(expected, _component.SelectedValues.ToList());
		}
	}
}