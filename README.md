# TableStorage.Abstractions.TableEntityConverters
[![Build status](https://ci.appveyor.com/api/projects/status/20rwpny4jfng24ws?svg=true)](https://ci.appveyor.com/project/giometrix/tablestorage-abstractions-tableentityconverters)
[![NuGet](https://img.shields.io/nuget/v/TableStorage.Abstractions.TableEntityConverters.svg)](https://www.nuget.org/packages/TableStorage.Abstractions.TableEntityConverters/)
[![Nuget Downloads](https://img.shields.io/nuget/dt/TableStorage.Abstractions.TableEntityConverters.svg?color=purple&logo=nuget)](https://www.nuget.org/packages/TableStorage.Abstractions.TableEntityConverters)

Easily convert POCOs (Plain Old CLR Objects) to Azure Table Storage TableEntities and vice versa

The Azure Storage SDK requires that objects that it works with to implement the ITableEntity interface.  This puts us into one of two places that are often not desirable:

1. You implement the ITableEntity interace, or inherit from TableEntity.  This is easy, but now you've got a leaky abstraction, as well as properties that won't make much sense in your domain (e.g. instead of a UserId, you've now got a RowKey, of the wrong type), or you have fields that are out of place, like ETag and Timestamp.
2. You create DTOs to save to ship data back and forth from the domain to Table Storage.  This is a common style, but often is overkill, especially if we're just looking for a simple abstraction on top of Azure Table Storage.

This simple library seeks to take care of the mapping for us, so that you can continue to write your domain objects as POCOs, while still being able to leverage the Azure Storage SDK.

The library will convert simple properties to fields in Azure Table Storage.  Complex types will serialize as json.

Examples
========
We'll use the following two classes for our examples

```csharp
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
    }
```

## Convert To Table Entity
Converting to a table entity is easy.  Use the ``.ToTableEntity()`` extension method and specify which properties represent the partition key and row key.  If you need to customize how any of those two keys serialize there are overloads that accept string values.

Example:
```csharp
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
                HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008")
            };
            var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
```

Below is an example that uses string keys instead:
```csharp
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
                HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008")
            };
            var tableEntity = emp.ToTableEntity("Google", "42");
```
### Ignoring Properties When Converting To Table Entity
When converting your POCO to a table entity and ultimately serializing and persisting to Azure Table Storage, you may want to ignore some fields.  To ignore properties, use the optional ```ignoredProperties``` parameter.

Example:
```csharp
 var tableEntity = emp.ToTableEntity(e=>e.Company, e=>e.Id, e=>e.ExternalId, e=>e.HireDate);
```
In the above example the partition key is ```Company```, the row key is ```Id``` and we ignored ```ExternalId``` and ```HireDate```.

## Convert From Table Entity
Converting from a table entity is just as simple.  If the both the partition keys can be converted to simple types, you can use the shorter overloaded extension method (```FromTableEntity```).

Here is a simple example where we specify the partition key (```Company```) and the row key (```Id```):
```csharp
 var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
```

Here is an example where a more complicated key was used, which is common in azure table storage because of the lack of indexes.
```csharp
var employee = tableEntity.FromTableEntity<Employee, string, int>(e=>e.Company, pk=>pk.Substring("company_".Length), e => e.Id, rk=>int.Parse(rk.Substring("employee_".Length)));
```
In this example the partitionkey had a prefix of "company_" and the row key had a prefix of "employee_".

### Converting From Table Entity While Ignoring Key Properties (Partition Key and Row Key)
When converting from a table entity, you may not want to populate any fields derived from `PartitionKey` and `RowKey`.  One reason for doing this might be that those keys are complex (derived from multiple properties for instance), and you already have those simple properties in your entity.

For your conveninece you can use the simplified `FromTableEntity` method.  This is the equivilant of doing 
```csharp
var employee = tableEntity.FromTableEntity<Employee,object,object>(null, null, null, null);
```

Example:
```csharp
var employee = tableEntity.FromTableEntity<Employee>();
```
