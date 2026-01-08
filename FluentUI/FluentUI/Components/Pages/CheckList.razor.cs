using FluentUI.Data;
using FluentUI.Data.Models;

using Microsoft.AspNetCore.Components;

namespace FluentUI.Components.Pages
{
	public partial class CheckList : ComponentBase
	{
		// Scenario 1: Employees
		public List<Employee> EmployeeList { get; set; } = DataHelper.GetMockEmployees();

		public List<string> SelectedEmployeeIds { get; set; } = [];
		public List<string> SelectedEmployeeNames { get; set; } = [];
		public string EmployeeIdsOutput { get; set; } = "";
		public string EmployeeNamesOutput { get; set; } = "";

		private void OnEmployeeIdsChanged(List<string> ids) =>
			EmployeeIdsOutput = string.Join(", ", ids);

		private void OnEmployeeNamesChanged(List<string> names) =>
			EmployeeNamesOutput = string.Join(", ", names);

		// Scenario 2: Simple Strings
		public List<string> ColorList { get; set; } = ["Red", "Green", "Blue", "Yellow"];

		public List<string> SelectedColors { get; set; } = [];
		public List<string> SelectedColorNames { get; set; } = [];
		public string ColorOutput { get; set; } = "";

		private void OnColorsChanged(List<string> values) =>
			ColorOutput = string.Join(", ", values);

		private void OnColorNamesChanged(List<string> names) =>
			ColorOutput = string.Join(", ", names);

		// Scenario 3: Text-only Objects
		public List<string> DepartmentList { get; set; } = ["HR", "Finance", "IT", "Marketing"];

		public List<string> SelectedDepartments { get; set; } = [];
		public List<string> SelectedDepartmentNames { get; set; } = [];
		public string DepartmentOutput { get; set; } = "";

		private void OnDepartmentsChanged(List<string> values) =>
			DepartmentOutput = string.Join(", ", values);

		private void OnDepartmentNamesChanged(List<string> names) =>
			DepartmentOutput = string.Join(", ", names);
	}
}