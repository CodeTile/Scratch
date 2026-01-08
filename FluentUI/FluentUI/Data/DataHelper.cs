using FluentUI.Data.Models;

namespace FluentUI.Data
{
	public static class DataHelper
	{
		public static List<Employee> GetMockEmployees()
		{
			return
			[
				new()
			{
				EmployeeId = 4,
				FirstName = "S Ravi",
				LastName = "Kumar",
				MobileNo = "9901091975",
				Email = "SKumar@gmail.com"
			},
			new()
				{
					EmployeeId = 6,
					FirstName = "Payal",
					LastName = "Jain",
					MobileNo = "9001091905",
					Email = "PJain@gmail.com"
				},
			new()
			{
				EmployeeId = 7,
				FirstName = "Alok",
				LastName = "Ojha",
				MobileNo = "900091905",
				Email = "AOjha@gmail.com"
			},
			new()
			{
				EmployeeId = 9,
				FirstName = "Divya",
				LastName = "Bharti",
				MobileNo = "9111767895",
				Email = "DBharti@gmail.com"
			},
			new()
				{
					EmployeeId = 10,
				FirstName = "Mayuri",
				LastName = "Kango",
				MobileNo = "9111000025",
				Email = "MKango@gmail.com"
			},
			new()
				{
					EmployeeId = 11,
				FirstName = "Tamraj",
				LastName = "Kilwish",
				MobileNo = "9251000025",
				Email = "TKilwish@gmail.com"
			},
			new()
				{
					EmployeeId = 12,
				FirstName = "James",
				LastName = "Bond",
				MobileNo = "9521000025",
				Email = "James007@gmail.com"
			}
			];
		}
	}
}