namespace FluentUI.Tests.Components.Shared;

using FluentUI.Components.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shouldly;

[TestClass]
public class CheckBoxListTests
{
	private class Employee
	{
		public long Id { get; set; }
		public string Name { get; set; } = "";
	}

	// ---------------------------------------------------------
	// Basic add/remove
	// ---------------------------------------------------------

	[TestMethod]
	public void AddSelection_ShouldAddValueAndText()
	{
		var component = NewComponent<string>();

		component.ToggleValue("101", "John Doe", true);

		component.SelectedValues.ShouldContain("101");
		component.SelectedTexts.ShouldContain("John Doe");
	}

	[TestMethod]
	public void RemoveSelection_ShouldRemoveValueAndText()
	{
		var component = NewComponent<string>();
		component.ToggleValue("101", "John Doe", true);

		component.ToggleValue("101", "John Doe", false);

		component.SelectedValues.ShouldNotContain("101");
		component.SelectedTexts.ShouldNotContain("John Doe");
	}

	// ---------------------------------------------------------
	// Multiple selections
	// ---------------------------------------------------------

	[TestMethod]
	public void MultipleSelections_ShouldAddAll()
	{
		var component = NewComponent<string>();

		component.ToggleValue("1", "A", true);
		component.ToggleValue("2", "B", true);
		component.ToggleValue("3", "C", true);

		component.SelectedValues.ShouldBe(["1", "2", "3"]);
		component.SelectedTexts.ShouldBe(["A", "B", "C"]);
	}

	[TestMethod]
	public void RemovingOneFromMultiple_ShouldOnlyRemoveThatOne()
	{
		var component = NewComponent<string>();

		component.ToggleValue("1", "A", true);
		component.ToggleValue("2", "B", true);
		component.ToggleValue("3", "C", true);

		component.ToggleValue("2", "B", false);

		component.SelectedValues.ShouldBe(["1", "3"]);
		component.SelectedTexts.ShouldBe(["A", "C"]);
	}

	// ---------------------------------------------------------
	// Duplicate prevention
	// ---------------------------------------------------------

	[TestMethod]
	public void DuplicateSelections_ShouldNotBeAdded()
	{
		var component = NewComponent<string>();

		component.ToggleValue("Blue", "Blue", true);
		component.ToggleValue("Blue", "Blue", true);

		component.SelectedValues.Count.ShouldBe(1);
		component.SelectedTexts.Count.ShouldBe(1);
	}

	// ---------------------------------------------------------
	// Repeated toggling
	// ---------------------------------------------------------

	[TestMethod]
	public void RepeatedToggle_ShouldEndInCorrectState()
	{
		var component = NewComponent<string>();

		component.ToggleValue("X", "X", true);
		component.ToggleValue("X", "X", false);
		component.ToggleValue("X", "X", true);

		component.SelectedValues.ShouldBe(["X"]);
		component.SelectedTexts.ShouldBe(["X"]);
	}

	// ---------------------------------------------------------
	// Null and empty handling
	// ---------------------------------------------------------

	[TestMethod]
	public void EmptyValue_ShouldBeHandled()
	{
		var component = NewComponent<string>();

		component.ToggleValue("", "", true);

		component.SelectedValues.ShouldContain("");
		component.SelectedTexts.ShouldContain("");
	}

	[TestMethod]
	public void NullValue_ShouldBeHandled()
	{
		var component = NewComponent<string>();

		component.ToggleValue(null!, null!, true);

		component.SelectedValues.ShouldContain(null);
		component.SelectedTexts.ShouldContain(null);
	}

	// ---------------------------------------------------------
	// Order preservation
	// ---------------------------------------------------------

	[TestMethod]
	public void OrderShouldBePreserved()
	{
		var component = NewComponent<string>();

		component.ToggleValue("A", "A", true);
		component.ToggleValue("B", "B", true);
		component.ToggleValue("C", "C", true);

		component.SelectedValues.ShouldBe(["A", "B", "C"]);
	}

	// ---------------------------------------------------------
	// Complex object tests
	// ---------------------------------------------------------

	[TestMethod]
	public void ComplexObject_ShouldStoreCorrectValues()
	{
		var component = NewComponent<Employee>();

		component.ToggleValue("101", "Alice", true);

		component.SelectedValues.ShouldContain("101");
		component.SelectedTexts.ShouldContain("Alice");
	}

	// ---------------------------------------------------------
	// Stress test
	// ---------------------------------------------------------

	[TestMethod]
	public void LargeNumberOfSelections_ShouldAllBeAdded()
	{
		var component = NewComponent<string>();

		for (int i = 0; i < 1000; i++)
		{
			component.ToggleValue(i.ToString(), $"Text{i}", true);
		}

		component.SelectedValues.Count.ShouldBe(1000);
		component.SelectedTexts.Count.ShouldBe(1000);
	}

	// ---------------------------------------------------------
	// Helper
	// ---------------------------------------------------------

	private static CheckBoxList<T> NewComponent<T>() =>
		new()
		{
			SelectedValues = new List<string>(),
			SelectedTexts = new List<string>()
		};
}