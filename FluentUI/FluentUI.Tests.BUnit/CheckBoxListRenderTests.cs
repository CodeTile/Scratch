namespace FluentUI.Tests.Components.Shared;

using FluentUI.Components.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Linq;

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

		Assert.IsTrue(component.SelectedValues.Any(x => x == "101"));
		Assert.IsTrue(component.SelectedTexts.Any(x => x == "John Doe"));
	}

	[TestMethod]
	public void RemoveSelection_ShouldRemoveValueAndText()
	{
		var component = NewComponent<string>();
		component.ToggleValue("101", "John Doe", true);

		component.ToggleValue("101", "John Doe", false);

		Assert.IsFalse(component.SelectedValues.Any(x => x == "101"));
		Assert.IsFalse(component.SelectedTexts.Any(x => x == "John Doe"));
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

		CollectionAssert.AreEqual(
			new[] { "1", "2", "3" },
			component.SelectedValues.ToList()
		);

		CollectionAssert.AreEqual(
			new[] { "A", "B", "C" },
			component.SelectedTexts.ToList()
		);
	}

	[TestMethod]
	public void RemovingOneFromMultiple_ShouldOnlyRemoveThatOne()
	{
		var component = NewComponent<string>();

		component.ToggleValue("1", "A", true);
		component.ToggleValue("2", "B", true);
		component.ToggleValue("3", "C", true);

		component.ToggleValue("2", "B", false);

		CollectionAssert.AreEqual(
			new[] { "1", "3" },
			component.SelectedValues.ToList()
		);

		CollectionAssert.AreEqual(
			new[] { "A", "C" },
			component.SelectedTexts.ToList()
		);
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

		Assert.AreEqual(1, component.SelectedValues.Count);
		Assert.AreEqual(1, component.SelectedTexts.Count);
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

		Assert.IsTrue(component.SelectedValues.Any(x => x == "X"));
		Assert.IsTrue(component.SelectedTexts.Any(x => x == "X"));
	}

	// ---------------------------------------------------------
	// Null and empty handling
	// ---------------------------------------------------------

	[TestMethod]
	public void EmptyValue_ShouldBeHandled()
	{
		var component = NewComponent<string>();

		component.ToggleValue("", "", true);

		Assert.IsTrue(component.SelectedValues.Any(x => x == ""));
		Assert.IsTrue(component.SelectedTexts.Any(x => x == ""));
	}

	[TestMethod]
	public void NullValue_ShouldBeHandled()
	{
		var component = NewComponent<string>();

		component.ToggleValue(null!, null!, true);

		Assert.IsTrue(component.SelectedValues.Any(x => x == null));
		Assert.IsTrue(component.SelectedTexts.Any(x => x == null));
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

		CollectionAssert.AreEqual(
			new[] { "A", "B", "C" },
			component.SelectedValues.ToList()
		);
	}

	// ---------------------------------------------------------
	// Complex object tests
	// ---------------------------------------------------------

	[TestMethod]
	public void ComplexObject_ShouldStoreCorrectValues()
	{
		var component = NewComponent<Employee>();

		component.ToggleValue("101", "Alice", true);

		Assert.IsTrue(component.SelectedValues.Any(x => x == "101"));
		Assert.IsTrue(component.SelectedTexts.Any(x => x == "Alice"));
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

		Assert.AreEqual(1000, component.SelectedValues.Count);
		Assert.AreEqual(1000, component.SelectedTexts.Count);
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