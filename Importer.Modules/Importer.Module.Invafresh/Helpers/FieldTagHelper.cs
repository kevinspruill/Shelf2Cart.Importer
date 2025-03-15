using Importer.Module.Invafresh.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Helpers
{
    public static class FieldTagHelper
    {
        public static Dictionary<string, object> GetFieldTagValues(object obj)
        {
            var result = new Dictionary<string, object>();

            foreach (var prop in obj.GetType().GetProperties())
            {
                var attr = prop.GetCustomAttribute<FieldTagAttribute>();
                if (attr != null)
                {
                    var value = prop.GetValue(obj);
                    if (value != null)
                    {
                        result[attr.Tag] = value;
                    }
                }
            }

            return result;
        }
        public static T GetTagValue<T>(Dictionary<string, string> fields, string tag, T defaultValue = default)
        {
            if (!fields.TryGetValue(tag, out var value) || string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)value;

                if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                {
                    if (int.TryParse(value, out var intVal))
                        return (T)(object)intVal;
                    return defaultValue;
                }

                if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                {
                    if (decimal.TryParse(value, out var decVal))
                        return (T)(object)decVal;
                    return defaultValue;
                }

                if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
                {
                    if (DateTime.TryParse(value, out var dateVal))
                        return (T)(object)dateVal;
                    return defaultValue;
                }

                if (typeof(T).IsEnum)
                {
                    // For enum types
                    try
                    {
                        return (T)Enum.Parse(typeof(T), value, true);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        public static T GetModelValueForTag<T>(object model, string tagName, T defaultValue = default)
        {
            // Find property with the matching tag attribute
            var property = model.GetType().GetProperties()
                .FirstOrDefault(p =>
                    p.GetCustomAttribute<FieldTagAttribute>()?.Tag == tagName);

            if (property == null)
                return defaultValue;

            // Get the value
            var value = property.GetValue(model);

            if (value == null)
                return defaultValue;

            try
            {
                // Try to convert to requested type
                if (typeof(T).IsAssignableFrom(value.GetType()))
                    return (T)value;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        public static Dictionary<string, string> GetTagValuesFromModel<T>(T model, string tag) where T : class
        {
            var result = new Dictionary<string, string>();

            foreach (var prop in typeof(T).GetProperties())
            {
                var attr = prop.GetCustomAttribute<FieldTagAttribute>();
                if (attr != null)
                {
                    var value = prop.GetValue(model);
                    if (value != null)
                    {
                        // Convert value to string representation
                        string stringValue;

                        if (value is Enum enumValue)
                            stringValue = enumValue.ToString();
                        else if (value is DateTime dateValue)
                            stringValue = dateValue.ToString("yyyy-MM-dd");
                        else
                            stringValue = value.ToString();

                        result[attr.Tag] = stringValue;
                    }
                }
            }

            return result;
        }
        public static void SetFieldTagValues(object obj, Dictionary<string, string> fields)
        {
            var props = obj.GetType().GetProperties()
                .Where(p => p.GetCustomAttribute<FieldTagAttribute>() != null)
                .ToDictionary(
                    p => p.GetCustomAttribute<FieldTagAttribute>().Tag,
                    p => p);

            foreach (var field in fields)
            {
                if (props.TryGetValue(field.Key, out var prop))
                {
                    // Convert string value to property type and set
                    object convertedValue = ConvertValue(field.Value, prop.PropertyType);
                    prop.SetValue(obj, convertedValue);
                }
            }
        }
        private static object ConvertValue(string value, Type targetType)
        {
            // Basic type conversion logic
            if (string.IsNullOrEmpty(value))
                return null;

            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                if (int.TryParse(value, out var intVal))
                    return intVal;
                return targetType == typeof(int?) ? (int?)null : 0;
            }

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                if (DateTime.TryParse(value, out var dateVal))
                    return dateVal;
                return targetType == typeof(DateTime?) ? (DateTime?)null : DateTime.MinValue;
            }

            // Add other type conversions as needed

            return null;
        }
    }
}
