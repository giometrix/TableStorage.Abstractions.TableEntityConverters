# TableStorage.Abstractions.TableEntityConverters
Easily convert POCOs (Plain Old CLR Objects) to Azure Table Storage TableEntities and vice versa

The Azure Storage SDK requires that objects that it works with to implement the ITableEntity interface.  This puts us into one of two places that are often not desirable:

1. You implement the ITableEntity interace, or inherit from TableEntity.  This is easy, but now you've got a leaky abstraction, as well as properties that won't make much sense in your domain (e.g. instead of a UserId, you've now got a RowKey, of the wrong type), or you have fields that are out of place, like ETag and Timestamp.
2. You create DTOs to save to ship data back and forth from the domain to Table Storage.  This is a common style, but often is overkill, especially if we're just looking for a simple abstraction on top of Azure Table Storage.

This simple library seeks to take care of the mapping for us, so that you can continue to write your domain objects as POCOs, while still being able to leverage the Azure Storage SDK.

Examples
========
We'll use the following two classes for our examples



