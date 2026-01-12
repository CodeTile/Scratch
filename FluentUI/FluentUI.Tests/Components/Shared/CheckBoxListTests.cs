using FluentUI.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;

namespace FluentUI.Tests.Components.Shared
{
	// Subclass to expose protected OnParametersSet for testing
	public class TestableCheckBoxList<TItem> : CheckBoxList<TItem>
	{
		public void OnParametersSetPublic() => base.OnParametersSet();
	}

	[TestClass]
	public class CheckBoxListTests
	{
		private class TestItem
		{
			public int Id { get; set; }
			public string Name { get; set; } = string.Empty;
		}

		[TestMethod]
		public void OnParametersSet_BuildsExcludedTextsHashSet_CaseInsensitive()
		{
			// Arrange
			var component = new TestableCheckBoxList<string>
			{
				ExcludedTexts = new[] { "One", "Two", "three", null!, " " }
			};

			// Act
			component.OnParametersSetPublic();

			// Access private field via reflection
			var excludedField = typeof(CheckBoxList<string>)
				.GetField("_excludedTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var excludedSet = (HashSet<string>)excludedField!.GetValue(component)!;

			// Assert
			Assert.IsTrue(excludedSet.Contains("one"));
			Assert.IsTrue(excludedSet.Contains("TWO"));
			Assert.IsTrue(excludedSet.Contains("Three"));
			Assert.AreEqual(3, excludedSet.Count); // null/whitespace ignored
		}

		[TestMethod]
		public void ToggleValue_AddsAndRemovesItems_AndInvokesCallbacks()
		{
			// Arrange
			var component = new CheckBoxList<string>();

			bool valuesCalled = false;
			bool textsCalled = false;

			// Assign EventCallbacks with test actions
			component.SelectedValuesChanged = new EventCallback<List<string>>(null,
				new Action<List<string>>(list => valuesCalled = true));

			component.SelectedTextsChanged = new EventCallback<List<string>>(null,
				new Action<List<string>>(list => textsCalled = true));

			string value = "val1";
			string text = "Text1";

			// Act: check the box
			component.ToggleValue(value, text, true);

			// Assert add
			Assert.IsTrue(component.SelectedValues.Contains(value));
			Assert.IsTrue(component.SelectedTexts.Contains(text));
			Assert.IsTrue(valuesCalled);
			Assert.IsTrue(textsCalled);

			// Reset callback flags
			valuesCalled = false;
			textsCalled = false;

			// Act: uncheck the box
			component.ToggleValue(value, text, false);

			// Assert remove
			Assert.IsFalse(component.SelectedValues.Contains(value));
			Assert.IsFalse(component.SelectedTexts.Contains(text));
			Assert.IsTrue(valuesCalled);
			Assert.IsTrue(textsCalled);
		}

		[TestMethod]
		public void DataAndValueFields_CanBeUsed()
		{
			// Arrange
			var items = new List<TestItem>
			{
				new() { Id = 1, Name = "Item1" },
				new() { Id = 2, Name = "Item2" }
			};

			var component = new CheckBoxList<TestItem>
			{
				Data = items,
				TextField = i => i.Name,
				ValueField = i => i.Id
			};

			// Act & Assert
			Assert.AreEqual(2, component.Data.Count());
			Assert.AreEqual("Item1", component.TextField!(items[0]));
			Assert.AreEqual(2, component.ValueField!(items[1]));
		}
	}
}