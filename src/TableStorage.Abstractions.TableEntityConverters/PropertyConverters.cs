using System;
using System.Collections.Generic;

namespace TableStorage.Abstractions.TableEntityConverters
{
	public class PropertyConverter<T>
	{
		public Func<T, object> ToTableEntityProperty { get; }
		public Action<T, object> SetObjectProperty { get; }

		public PropertyConverter(Func<T, object> toTableEntityProperty, Action<T, object> setObjectProperty)
		{
			ToTableEntityProperty = toTableEntityProperty;
			SetObjectProperty = setObjectProperty;
		}
		
	}
	public class PropertyConverters<T> : Dictionary<string, PropertyConverter<T>>
	{
		
	}
}