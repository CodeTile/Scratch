namespace FluentUI.Tests.Components.Shared;

using FluentUI.Components.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shouldly;

using System.Collections.Generic;

[TestClass]
public class CheckBoxListTests
{
	// ---------------------------------------------------------
	// Scenario 1: Complex object model
	// ---------------------------------------------------------
	private class Employee
	{
		public long Id { get; set; }
		public string Name { get; set; } = "";
	}

	[TestMethod]
	public void ToggleValue_Should_Add_ComplexObject_Selection()
	{
		var component = new CheckBoxList<Employee>
		{
			SelectedValues = new List<string>(),
			SelectedTexts = new List<string>()
		};

		component.ToggleValue("101", "John Doe", true);

		component.SelectedValues.ShouldContain("101");
		component.SelectedTexts.ShouldContain("John Doe");
	}

	[TestMethod]
	public void ToggleValue_Should_Remove_ComplexObject_Selection()
	{
		var component = new CheckBoxList<Employee>
		{
			SelectedValues = new List<string> { "101" },
			SelectedTexts = new List<string> { "John Doe" }
		};

		component.ToggleValue("101", "John Doe", false);

		component.SelectedValues.ShouldNotContain("101");
		component.SelectedTexts.ShouldNotContain("John Doe");
	}

	// ---------------------------------------------------------
	// Scenario 2: Simple string list
	// ---------------------------------------------------------
	[TestMethod]
	public void ToggleValue_Should_Add_String_Selection()
	{
		var component = new CheckBoxList<string>
		{
			SelectedValues = new List<string>(),
			SelectedTexts = new List<string>()
		};

		component.ToggleValue("Red", "Red", true);

		component.SelectedValues.ShouldContain("Red");
		component.SelectedTexts.ShouldContain("Red");
	}

	[TestMethod]
	public void ToggleValue_Should_Remove_String_Selection()
	{
		var component = new CheckBoxList<string>
		{
			SelectedValues = new List<string> { "Red" },
			SelectedTexts = new List<string> { "Red" }
		};

		component.ToggleValue("Red", "Red", false);

		component.SelectedValues.ShouldNotContain("Red");
		component.SelectedTexts.ShouldNotContain("Red");
	}

	// ---------------------------------------------------------
	// Scenario 3: Text-only objects (value == text)
	// ---------------------------------------------------------
	[TestMethod]
	public void ToggleValue_Should_Add_TextOnly_Selection()
	{
		var component = new CheckBoxList<string>
		{
			SelectedValues = new List<string>(),
			SelectedTexts = new List<string>()
		};

		component.ToggleValue("Finance", "Finance", true);

		component.SelectedValues.ShouldContain("Finance");
		component.SelectedTexts.ShouldContain("Finance");
	}

	// ---------------------------------------------------------
	// Duplicate protection
	// ---------------------------------------------------------
	[TestMethod]
	public void ToggleValue_Should_Not_Duplicate_Selections()
	{
		var component = new CheckBoxList<string>
		{
			SelectedValues = new List<string> { "Blue" },
			SelectedTexts = new List<string> { "Blue" }
		};

		component.ToggleValue("Blue", "Blue", true);

		component.SelectedValues.Count.ShouldBe(1);
		component.SelectedTexts.Count.ShouldBe(1);
	}

	// ---------------------------------------------------------
	// Add/remove sequence
	// ---------------------------------------------------------
	[TestMethod]
	public void ToggleValue_Should_Add_Then_Remove_Correctly()
	{
		var component = new CheckBoxList<string>
		{
			SelectedValues = new List<string>(),
			SelectedTexts = new List<string>()
		};

		component.ToggleValue("Green", "Green", true);
		component.ToggleValue("Green", "Green", false);

		component.SelectedValues.ShouldBeEmpty();
		component.SelectedTexts.ShouldBeEmpty();
	}
}