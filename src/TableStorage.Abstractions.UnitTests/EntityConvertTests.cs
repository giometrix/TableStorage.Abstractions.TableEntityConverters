
using System;
using TableStorage.Abstractions.TableEntityConverters;
using Xunit;

namespace TableStorage.Abstractions.UnitTests
{
   
    public class EntityConvertTests
    {
       [Fact]
        public void convert_to_entity_table()
        {
            var emp = new Employee()
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
            var te = emp.ToTableEntity(e => e.Company, e => e.Id);
           
            Assert.Equal(true, te.Properties.ContainsKey("DepartmentJson"));
        }


        [Fact]
        public void convert_to_entity_table_explicit_keys()
        {
            var emp = new Employee()
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
            var te = emp.ToTableEntity("Google", "42");
            Assert.Equal("Google", te.PartitionKey);
        }


        [Fact]
        public void convert_from_entity_table()
        {
            var emp = new Employee()
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
            var te = emp.ToTableEntity(e => e.Company, e => e.Id);
            var employee = te.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
            Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
        }

        [Fact]
        public void convert_from_entity_table_complex_key()
        {
            var emp = new Employee()
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
            var employee = te.FromTableEntity<Employee, string, int>(e=>e.Company, pk=>pk.Substring("company_".Length), e => e.Id, rk=>int.Parse(rk.Substring("employee_".Length)));

            Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
        }

        [Fact]
        public void convert_from_entity_table_unmapped_partition_key()
        {
            var emp = new Employee()
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
            var employee = te.FromTableEntity<Employee, string, int>(null, null, e => e.Id, rk => int.Parse(rk.Substring("employee_".Length)));

            Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
        }
    }
}
