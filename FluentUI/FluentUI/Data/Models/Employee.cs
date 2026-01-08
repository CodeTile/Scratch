namespace FluentUI.Data.Models
{
	public class Employee
	{
		public long EmployeeId
		{ get; set; }

		public string FirstName
		{ get; set; }

		public string LastName
		{ get; set; }

		public string FullName
		{
			get
			{
				return FirstName + " " + LastName;
			}
		}
		public string MobileNo
		{ get; set; }
		public string Email
		{ get; set; }
	}
}
