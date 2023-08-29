# TableStorage.Abstractions.TableEntityConverters
[![Build status](https://ci.appveyor.com/api/projects/status/20rwpny4jfng24ws?svg=true)](https://ci.appveyor.com/project/giometrix/tablestorage-abstractions-tableentityconverters)
[![NuGet](https://img.shields.io/nuget/v/TableStorage.Abstractions.TableEntityConverters.svg)](https://www.nuget.org/packages/TableStorage.Abstractions.TableEntityConverters/)
[![Nuget Downloads](https://img.shields.io/nuget/dt/TableStorage.Abstractions.TableEntityConverters.svg?color=purple&logo=nuget)](https://www.nuget.org/packages/TableStorage.Abstractions.TableEntityConverters)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/1dc6fc21b5b84de0a059ad3b527f2136)](https://app.codacy.com/gh/giometrix/TableStorage.Abstractions.TableEntityConverters/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)

Easily convert POCOs (Plain Old CLR Objects) to Azure Table Storage TableEntities and vice versa

The Azure Storage SDK requires that objects that it works with to implement the ITableEntity interface.  This puts us into one of two places that are often not desirable:

1. You implement the ITableEntity interace, or inherit from TableEntity.  This is easy, but now you've got a leaky abstraction, as well as properties that won't make much sense in your domain (e.g. instead of a UserId, you've now got a RowKey, of the wrong type), or you have fields that are out of place, like ETag and Timestamp.
2. You create DTOs to save to ship data back and forth from the domain to Table Storage.  This is a common style, but often is overkill, especially if we're just looking for a simple abstraction on top of Azure Table Storage.

This simple library seeks to take care of the mapping for us, so that you can continue to write your domain objects as POCOs, while still being able to leverage the Azure Storage SDK.

The library will convert simple properties to fields in Azure Table Storage.  Complex types will serialize as json.

## :bangbang: Important Note About Versioning
`TableStorage.Abstractions.TableEntityConverters` uses semantic versioning.  Anything changes to a major release should not be breaking, e.g. upgrading to 1.5 from 1.4 should not require a code change.

The upgrade from 1.5 to 2.0 does not introduce changes needed in your use of `TableStorage.Abstractions.TableEntityConverters`, however the underlying table storage SDK is now using the newer [Azure.Data.Tables](https://www.nuget.org/packages/Azure.Data.Tables/) instead of the older [Microsoft.Azure.Cosmos.Table](https://www.nuget.org/packages/Microsoft.Azure.Cosmos.Table/) SDK.

If you directly use Microsoft's Table Storage SDK, you will need to use `Azure.Data.Tables`.  It should not require much change, but nevertheless it is a change.  If you do not want to upgrade at this time, stick with `TableStorage.Abstractions.TableEntityConverters` 1.5.


Examples
========
We'll use the following two classes for our examples

```c#
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
```c#
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
```c#
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
```c#
 var tableEntity = emp.ToTableEntity(e=>e.Company, e=>e.Id, e=>e.ExternalId, e=>e.HireDate);
```
In the above example the partition key is ```Company```, the row key is ```Id``` and we ignored ```ExternalId``` and ```HireDate```.

## Convert From Table Entity
Converting from a table entity is just as simple.  If the both the partition keys can be converted to simple types, you can use the shorter overloaded extension method (```FromTableEntity```).

Here is a simple example where we specify the partition key (```Company```) and the row key (```Id```):
```c#
 var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
```

Here is an example where a more complicated key was used, which is common in azure table storage because of the lack of indexes.
```c#
var employee = tableEntity.FromTableEntity<Employee, string, int>(e=>e.Company, pk=>pk.Substring("company_".Length), e => e.Id, rk=>int.Parse(rk.Substring("employee_".Length)));
```
In this example the partitionkey had a prefix of "company_" and the row key had a prefix of "employee_".

### Converting From Table Entity While Ignoring Key Properties (Partition Key and Row Key)
When converting from a table entity, you may not want to populate any fields derived from `PartitionKey` and `RowKey`.  One reason for doing this might be that those keys are complex (derived from multiple properties for instance), and you already have those simple properties in your entity.

For your conveninece you can use the simplified `FromTableEntity` method.  This is the equivilant of doing 
```c#
var employee = tableEntity.FromTableEntity<Employee,object,object>(null, null, null, null);
```

Example:
```c#
var employee = tableEntity.FromTableEntity<Employee>();
```

## Custom Json Serialization Settings For Complex Types
Beginning with v1.4, you can now control how the json for complex types are serialized/deserialized.

To set the settings globally, use `SetDefaultJsonSerializerSettings`.  Use this option if you want to apply them to all table entity conversions.

The other option is pass in the settingds in the newly overloaded `ToTableEntity` and `FromTableEntity` methods.

## Custom Property Conversion For Non-Key Fields
Starting in 1.5 you can specify custom property converters for properties that are not used as Partition or Row Key fields.

This is a niche use case, but useful if you need it, for example, if dates are stored as strings in Azure Table Storage.

Here is the object we'll be using in the example:
```c#
var car = new Car {
    Id = "abc",
    Make = "BMW",
    Model = "M5",
    Year = 2022,
    ReleaseDate = new DateTime(2022, 3, 1)
};

```

First we need to specify property converters.  `PropertyConverters` is a dictionary.  The key is the 
property name and the value is a `PropertyConverter`, which specifies how to convert to and from `EntityProperty`.

```c#
var propertyConverters = new PropertyConverters<Car> {
    [nameof(Car.ReleaseDate)] =
        new PropertyConverter<Car>(x => 
                car.ReleaseDate.ToString("yyyy-M-d"),
            (c,p) => c.ReleaseDate = p.ToString())
         )
};
```
Finally, pass the `PropertyConverters` object when converting to and from your table entities.

Note that in production use cases you don't have to always instantiate your property converters, you should have a single instance and re-use.

```c#
var jsonSerializerSettings = new JsonSerializerSettings();

var carEntity =
    car.ToTableEntity(c => c.Year, c => car.Id, jsonSerializerSettings, propertyConverters);

var fromEntity = carEntity.FromTableEntity<Car,int,string>(c=>c.Year, c=>c.Id, jsonSerializerSettings, propertyConverters);
```
