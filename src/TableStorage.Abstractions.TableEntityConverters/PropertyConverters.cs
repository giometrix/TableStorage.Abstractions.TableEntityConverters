using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;

namespace TableStorage.Abstractions.TableEntityConverters
{
	public class PropertyConverter<T>
	{
		public Func<T, EntityProperty> ToTableEntityProperty { get; }
		public Action<T,EntityProperty> SetObjectProperty { get; }

		public PropertyConverter(Func<T, EntityProperty> toTableEntityProperty, Action<T, EntityProperty> setObjectProperty)
		{
			ToTableEntityProperty = toTableEntityProperty;
			SetObjectProperty = setObjectProperty;
		}
		
	}
	public class PropertyConverters<T> : Dictionary<string, PropertyConverter<T>>
	{
		
	}
}