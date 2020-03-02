using System;

namespace TableStorage.Abstractions.UnitTests
{
	public class Department
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public Guid? OptionalId { get; set; }
	}

	public class Employee
	{
		public string Company { get; set; }
		public int Id { get; set; }
		public Guid ExternalId { get; set; }
		public string Name { get; set; }
		public DateTimeOffset HireDate { get; set; }
		public DateTimeOffset? TermDate { get; set; }
		public Department Department { get; set; }

		// extra stuff
		public DateTime ADateTime { get; set; }
		public DateTime? ANullableDateTime { get; set; }
		public int? ANullableInt { get; set; }
	}

	public class EmployeeWithTimestamp : Employee
	{
		public DateTimeOffset Timestamp { get; set; }
	}

	public class EmployeeWithTimestampAsString : Employee
	{
		public string Timestamp { get; set; }
	}
}