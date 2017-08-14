using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public Employee()
        {
            
        }
        public string Company { get; set; }
        public int Id { get; set; }
        public Guid ExternalId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset HireDate { get; set; }
        public DateTimeOffset? TermDate { get; set; }
        public Department Department { get; set; }
    }
}
