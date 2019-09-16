using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TableStorage.Abstractions.TableEntityConverters
{
	public static class EntityConvert
	{
		public static DynamicTableEntity ToTableEntity<T>(this T o, string partitionKey, string rowKey, params Expression<Func<T, object>>[] ignoredProperties)
		{
			var type = typeof(T);
			var properties = GetProperties(type);
			RemoveIgnoredProperties(properties, ignoredProperties);
			return CreateTableEntity(o, properties, partitionKey, rowKey);
		}

		public static DynamicTableEntity ToTableEntity<T>(this T o, Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty, params Expression<Func<T, object>>[] ignoredProperties)
		{
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

			return CreateTableEntity(o, properties, partitionKey, rowKey);
		}

		public static T FromTableEntity<T, TP, TR>(this DynamicTableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty) where T : new()
		{
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
				rowProperty, convertRow);
		}

		public static T FromTableEntity<T, TP, TR>(this DynamicTableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Func<string, TP> convertPartitionKey, Expression<Func<T, object>> rowProperty,
			Func<string, TR> convertRowKey) where T : new()
		{
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
			FillProperties(entity, o, properties);
			return o;
		}

		public static T FromTableEntity<T>(this DynamicTableEntity entity) where T : new()
		{
			return entity.FromTableEntity<T, object, object>(null, null, null, null);
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

		private static void FillProperties<T>(DynamicTableEntity entity, T o, List<PropertyInfo> properties) where T : new()
		{
			foreach (var propertyInfo in properties)
			{
				if (entity.Properties.ContainsKey(propertyInfo.Name))
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
						var propVal = JsonConvert.DeserializeObject(val, propertyInfo.PropertyType);
						propertyInfo.SetValue(o, propVal);
					}
				}
			}
		}

		private static DynamicTableEntity CreateTableEntity(object o, List<PropertyInfo> properties,
			string partitionKey, string rowKey)
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
						entityProperty = new EntityProperty(JsonConvert.SerializeObject(val));
						break;
				}
				entity.Properties[name] = entityProperty;
			}
			return entity;
		}

		private static List<PropertyInfo> GetProperties(Type type)
		{
			var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public)
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
