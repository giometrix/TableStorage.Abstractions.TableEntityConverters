using System;
using TableStorage.Abstractions.TableEntityConverters;
using Xunit;

namespace TableStorage.Abstractions.UnitTests
{
	public class EntityConvertTests
	{
		public class GuidKeyTest
		{
			public Guid A { get; set; }
			public Guid B { get; set; }
		}


		[Fact]
		public void convert_from_entity_table()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_complex_key()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity($"company_{emp.Company}", $"employee_{emp.Id}");
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company,
				pk => pk.Substring("company_".Length), e => e.Id, rk => int.Parse(rk.Substring("employee_".Length)));

			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_unmapped_partition_key()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var te = emp.ToTableEntity($"company_{emp.Company}", $"employee_{emp.Id}");
			var employee = te.FromTableEntity<Employee, string, int>(null, null, e => e.Id,
				rk => int.Parse(rk.Substring("employee_".Length)));

			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_unmapped_partition_key_and_unmapped_row_key()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var te = emp.ToTableEntity($"company_{emp.Company}", $"employee_{emp.Id}");
			var employee = te.FromTableEntity<Employee>();

			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}


		[Fact]
		public void convert_from_entity_table_with_datetime()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	"),
				ADateTime = DateTime.Parse("Wednesday, January 31, 2018")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_with_guid_keys()
		{
			var a = Guid.Parse("7ba5bd25-823e-4c01-940e-1f131cbed8ed");
			var b = Guid.Parse("603e51de-950e-4270-a755-c26950742103");

			var obj = new GuidKeyTest {A = a, B = b};
			var e = obj.ToTableEntity(x => x.A, x => x.B);
			var convertedObject = e.FromTableEntity<GuidKeyTest, Guid, Guid>(x => x.A, x => x.B);


			Assert.Equal(a, convertedObject.A);
		}


		[Fact]
		public void convert_from_entity_table_with_nullable_datetime_with_value()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	"),
				ANullableDateTime = DateTime.Parse("Wednesday, January 31, 2018")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_with_nullable_datetimeoffset_with_value()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	"),
				TermDate = DateTimeOffset.Parse("Wednesday, January 31, 2018")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}


		[Fact]
		public void convert_from_entity_table_with_nullable_int_with_value()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	"),
				ANullableDateTime = DateTime.Parse("Wednesday, January 31, 2018"),
				ANullableInt = 42
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_to_entity_table()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);

			Assert.Equal(true, tableEntity.Properties.ContainsKey("DepartmentJson"));
		}

		[Fact]
		public void convert_to_entity_table_explicit_keys()
		{
			var emp = new Employee
			{
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity("Google", "42");
			Assert.Equal("Google", tableEntity.PartitionKey);
		}


		[Fact]
		public void convert_to_entity_table_ignore_complex_properties()
		{
			var emp = new Employee
			{
				Company = "Google",
				Name = "John Smith",
				Id = 1,
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id, e => e.Department);
			Assert.Equal("Google", tableEntity.PartitionKey);
			Assert.True(tableEntity.Properties.ContainsKey("ExternalId"));
			Assert.True(tableEntity.Properties.ContainsKey("HireDate"));
			Assert.False(tableEntity.Properties.ContainsKey("DepartmentJson"));
		}


		[Fact]
		public void convert_to_entity_table_ignore_simple_properties()
		{
			var emp = new Employee
			{
				Company = "Google",
				Name = "John Smith",
				Id = 1,
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id, e => e.ExternalId, e => e.HireDate);
			Assert.Equal("Google", tableEntity.PartitionKey);
			Assert.False(tableEntity.Properties.ContainsKey("ExternalId"));
			Assert.False(tableEntity.Properties.ContainsKey("HireDate"));
		}

		[Fact]
		public void convert_to_entity_table_with_explicit_Keys_with_ignored_simple_properties()
		{
			var emp = new Employee
			{
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity("Google", "42", e => e.ExternalId, e => e.HireDate);
			Assert.Equal("Google", tableEntity.PartitionKey);
			Assert.False(tableEntity.Properties.ContainsKey("ExternalId"));
			Assert.False(tableEntity.Properties.ContainsKey("HireDate"));
		}
	}
}