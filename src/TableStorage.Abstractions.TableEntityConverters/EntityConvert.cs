using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Azure.Cosmos.Table;

namespace TableStorage.Abstractions.TableEntityConverters
{
	public static class EntityConvert
	{
		private static JsonSerializerSettings _defaultJsonSerializerSettings = new JsonSerializerSettings();

		/// <summary>
		/// Json fields will use be serialized/deserialized with these provided settings when jsonSerializerSettings are
		/// not explicitly passed into ToTableEntity/FromTableEntity
		/// </summary>
		/// <param name="jsonSerializerSettings">Note: null resets to default</param>
		public static void SetDefaultJsonSerializerSettings (JsonSerializerSettings jsonSerializerSettings = default)
		{
			_defaultJsonSerializerSettings = jsonSerializerSettings ?? new JsonSerializerSettings();
		}

		public static DynamicTableEntity ToTableEntity<T>(this T o, string partitionKey, string rowKey,
			params Expression<Func<T, object>>[] ignoredProperties)
		{
			return ToTableEntity(o, partitionKey, rowKey, _defaultJsonSerializerSettings, ignoredProperties);
		}
		public static DynamicTableEntity ToTableEntity<T>(this T o, string partitionKey, string rowKey, JsonSerializerSettings jsonSerializerSettings, params Expression<Func<T, object>>[] ignoredProperties)
		{
			_ = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
			var type = typeof(T);
			var properties = GetProperties(type);
			RemoveIgnoredProperties(properties, ignoredProperties);
			return CreateTableEntity(o, properties, partitionKey, rowKey, jsonSerializerSettings);
		}

		public static DynamicTableEntity ToTableEntity<T>(this T o, Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty, params Expression<Func<T, object>>[] ignoredProperties)
		{
			return ToTableEntity(o, partitionProperty, rowProperty, _defaultJsonSerializerSettings, ignoredProperties);
		}
		
		public static DynamicTableEntity ToTableEntity<T>(this T o, Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty, JsonSerializerSettings jsonSerializerSettings, params Expression<Func<T, object>>[] ignoredProperties)
		{
			_ = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
			var type = typeof(T);
			var properties = GetProperties(type);
			var partitionProp =
				properties.SingleOrDefault(p => p.Name == GetPropertyNameFromExpression(partitionProperty));
			if (partitionProp == null)
			{
				throw new ArgumentException(nameof(partitionProperty));
			}

			var rowProp = properties.SingleOrDefault(p => p.Name == GetPropertyNameFromExpression(rowProperty));
			if (rowProp == null)
			{
				throw new ArgumentException(nameof(rowProperty));
			}

			properties.Remove(partitionProp);
			properties.Remove(rowProp);
			RemoveIgnoredProperties(properties, ignoredProperties);

			var partitionKey = partitionProp.GetValue(o).ToString();
			var rowKey = rowProp.GetValue(o).ToString();

			return CreateTableEntity(o, properties, partitionKey, rowKey, jsonSerializerSettings);
		}

		public static T FromTableEntity<T, TP, TR>(this DynamicTableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty) where T : new()
		{
			return FromTableEntity<T, TP, TR>(entity, partitionProperty, rowProperty, _defaultJsonSerializerSettings);
		}

		public static T FromTableEntity<T, TP, TR>(this DynamicTableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty, JsonSerializerSettings jsonSerializerSettings) where T : new()
		{
			_ = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
			
			var convertPartition = new Func<string, TP>(p => (TP)Convert.ChangeType(p, typeof(TP)));
			var convertRow = new Func<string, TR>(r => (TR)Convert.ChangeType(r, typeof(TR)));

			if (typeof(TP) == typeof(Guid))
			{
				convertPartition = p => (TP)(object)Guid.Parse(p);
			}
			if (typeof(TR) == typeof(Guid))
			{
				convertRow = r => (TR)(object)Guid.Parse(r);
			}
			return FromTableEntity(entity, partitionProperty, convertPartition,
				rowProperty, convertRow, jsonSerializerSettings);
		}

		public static T FromTableEntity<T, TP, TR>(this DynamicTableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Func<string, TP> convertPartitionKey, Expression<Func<T, object>> rowProperty,
			Func<string, TR> convertRowKey) where T : new()
		{
			return FromTableEntity(entity, partitionProperty, convertPartitionKey, rowProperty,
				convertRowKey, _defaultJsonSerializerSettings);
		}
		
		public static T FromTableEntity<T, TP, TR>(this DynamicTableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Func<string, TP> convertPartitionKey, Expression<Func<T, object>> rowProperty,
			Func<string, TR> convertRowKey, JsonSerializerSettings jsonSerializerSettings) where T : new()
		{
			_ = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
			
			var o = new T();
			var type = typeof(T);
			var properties = GetProperties(type);
			var partitionPropName = partitionProperty != null ? GetPropertyNameFromExpression(partitionProperty) : null;
			var rowPropName = rowProperty != null ? GetPropertyNameFromExpression(rowProperty) : null;
			if (partitionPropName != null && convertPartitionKey != null)
			{
				var partitionProp = properties.SingleOrDefault(p => p.Name == partitionPropName);
				if (partitionProp == null)
				{
					throw new ArgumentException(nameof(partitionProperty));
				}

				partitionProp.SetValue(o, convertPartitionKey(entity.PartitionKey));
				properties.Remove(partitionProp);
			}
			if (rowPropName != null && convertRowKey != null)
			{
				var rowProp = properties.SingleOrDefault(p => p.Name == rowPropName);
				if (rowProp == null)
				{
					throw new ArgumentException(nameof(rowProperty));
				}

				rowProp.SetValue(o, convertRowKey(entity.RowKey));
				properties.Remove(rowProp);
			}

			SetTimestamp(entity, o, properties);
			FillProperties(entity, o, properties, jsonSerializerSettings);
			return o;
		}

		public static T FromTableEntity<T>(this DynamicTableEntity entity) where T : new()
		{
			return FromTableEntity<T>(entity, _defaultJsonSerializerSettings);
		}
		
		public static T FromTableEntity<T>(this DynamicTableEntity entity, JsonSerializerSettings jsonSerializerSettings) where T : new()
		{
			_ = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
			
			return entity.FromTableEntity<T, object, object>(null, null, null, null, jsonSerializerSettings);
		}

		internal static string GetPropertyNameFromExpression<T>(Expression<Func<T, object>> exp)
		{
			var name = "";

			var body = exp.Body as MemberExpression;
			if (body == null)
			{
				var ubody = (UnaryExpression)exp.Body;
				body = ubody.Operand as MemberExpression;
				name = body.Member.Name;
			}
			else
			{
				name = body.Member.Name;
			}

			return name;
		}

		private static void SetTimestamp<T>(DynamicTableEntity entity, T o, List<PropertyInfo> properties) where T : new()
		{
			var timestampProperty = properties
					.FirstOrDefault(p => p.Name == nameof(DynamicTableEntity.Timestamp));

			if (timestampProperty != null)
			{
				if(timestampProperty.PropertyType == typeof(DateTimeOffset))
				{
					timestampProperty.SetValue(o, entity.Timestamp);
				}

				if (timestampProperty.PropertyType == typeof(DateTime))
				{
					timestampProperty.SetValue(o, entity.Timestamp.UtcDateTime);
				}

				if (timestampProperty.PropertyType == typeof(string))
				{
					timestampProperty.SetValue(o, entity.Timestamp.ToString());
				}
			}
		}

		private static void FillProperties<T>(DynamicTableEntity entity, T o, List<PropertyInfo> properties, JsonSerializerSettings jsonSerializerSettings) where T : new()
		{
			foreach (var propertyInfo in properties)
			{
				if (entity.Properties.ContainsKey(propertyInfo.Name) && propertyInfo.Name != nameof(DynamicTableEntity.Timestamp))
				{
					var val = entity.Properties[propertyInfo.Name].PropertyAsObject;

					if (val != null && (propertyInfo.PropertyType == typeof(DateTimeOffset) || propertyInfo.PropertyType == typeof(DateTimeOffset?)))
					{
						val = entity.Properties[propertyInfo.Name].DateTimeOffsetValue;
					}

					if (val != null && propertyInfo.PropertyType == typeof(double))
					{
						val = entity.Properties[propertyInfo.Name].DoubleValue;
					}

					if (val != null && propertyInfo.PropertyType == typeof(int))
					{
						val = entity.Properties[propertyInfo.Name].Int32Value;
					}

					if (val != null && propertyInfo.PropertyType == typeof(long))
					{
						val = entity.Properties[propertyInfo.Name].Int64Value;
					}

					if (val != null && propertyInfo.PropertyType == typeof(Guid))
					{
						val = entity.Properties[propertyInfo.Name].GuidValue;
					}

					propertyInfo.SetValue(o, val);
				}
				else if (entity.Properties.ContainsKey($"{propertyInfo.Name}Json"))
				{
					var val = entity.Properties[$"{propertyInfo.Name}Json"].StringValue;
					if (val != null)
					{
						var propVal = JsonConvert.DeserializeObject(val, propertyInfo.PropertyType, jsonSerializerSettings ?? _defaultJsonSerializerSettings);
						propertyInfo.SetValue(o, propVal);
					}
				}
			}
		}

		private static DynamicTableEntity CreateTableEntity(object o, List<PropertyInfo> properties,
			string partitionKey, string rowKey, JsonSerializerSettings jsonSerializerSettings)
		{
			var entity = new DynamicTableEntity(partitionKey, rowKey);
			foreach (var propertyInfo in properties)
			{
				var name = propertyInfo.Name;
				var val = propertyInfo.GetValue(o);
				EntityProperty entityProperty;
				switch (val)
				{
					case int x:
						entityProperty = new EntityProperty(x);
						break;
					case short x:
						entityProperty = new EntityProperty(x);
						break;
					case byte x:
						entityProperty = new EntityProperty(x);
						break;
					case string x:
						entityProperty = new EntityProperty(x);
						break;
					case double x:
						entityProperty = new EntityProperty(x);
						break;
					case DateTime x:
						entityProperty = new EntityProperty(x);
						break;
					case DateTimeOffset x:
						entityProperty = new EntityProperty(x);
						break;
					case bool x:
						entityProperty = new EntityProperty(x);
						break;
					case byte[] x:
						entityProperty = new EntityProperty(x);
						break;
					case long x:
						entityProperty = new EntityProperty(x);
						break;
					case Guid x:
						entityProperty = new EntityProperty(x);
						break;
					case null:
						entityProperty = new EntityProperty((int?)null);
						break;
					default:
						name += "Json";
						entityProperty = new EntityProperty(JsonConvert.SerializeObject(val, jsonSerializerSettings ?? _defaultJsonSerializerSettings));
						break;
				}
				entity.Properties[name] = entityProperty;
			}
			return entity;
		}

		private static List<PropertyInfo> GetProperties(Type type)
		{
			var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p=>p.CanRead && p.CanWrite)
				.ToList();
			return properties;
		}

		private static void RemoveIgnoredProperties<T>(List<PropertyInfo> properties,
			Expression<Func<T, object>>[] ignoredProperties)
		{
			if (ignoredProperties != null)
			{
				for (int i = 0; i < ignoredProperties.Length; i++)
				{
					var ignoredProperty = properties.SingleOrDefault(p => p.Name == GetPropertyNameFromExpression(ignoredProperties[i]));
					properties.Remove(ignoredProperty);
				}
			}
		}
	}
}
