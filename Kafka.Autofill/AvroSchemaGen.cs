using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Kafka.Autofill;

internal static class AvroSchemaGen
{
    private static readonly HashSet<Type> ArrayTypes =
    [
        typeof(HashSet<>),
        typeof(List<>),
        typeof(IEnumerable<>),
        typeof(ICollection<>),
        typeof(IList<>)
    ];

    private static readonly HashSet<Type> MapTypes =
    [
        typeof(Dictionary<,>),
        typeof(IDictionary<,>)
    ];
    
    public static string GenerateSchema(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        var definedTypes = new HashSet<string>();
        var fields = GenerateFields(type, definedTypes);

        var schema = new Dictionary<string, object>
        {
            ["type"] = "record",
            ["name"] = type.Name,
            ["namespace"] = type.Namespace ?? string.Empty,
            ["fields"] = fields
        };

        return JsonConvert.SerializeObject(schema, Formatting.Indented);
    }

    private static IEnumerable<Dictionary<string, object>> GenerateFields(Type type, HashSet<string> definedTypes)
    {
        var avroIgnoreAttr = Type.GetType("Kafka.Autofill.AvroIgnoreAttribute")!;
        var publicFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var fields = publicFields
            .Where(field => field.GetCustomAttribute(avroIgnoreAttr) == null)
            .Select(field => new Dictionary<string, object>
            {
                ["name"] = field.Name,
                ["type"] = MapCSharpTypeToAvro(field.FieldType, IsNullable(field), definedTypes)
            });
        
        var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var properties = publicProperties
            .Where(property => property.GetCustomAttribute(avroIgnoreAttr) == null)
            .Select(property => new Dictionary<string, object>
            {
                ["name"] = property.Name, 
                ["type"] = MapCSharpTypeToAvro(property.PropertyType, IsNullable(property), definedTypes)
            });

        return fields.Concat(properties);
    }

    private static object MapCSharpTypeToAvro(Type type, bool isNullableFromContext, HashSet<string> definedTypes)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        var isNullableValueType = underlyingType != null;
        var actualType = underlyingType ?? type;
        
        var isNullable = isNullableValueType || (isNullableFromContext && !actualType.IsValueType);
        
        if (isNullable)
        {
            return new[] { "null", MapCSharpTypeToAvro(actualType, false, definedTypes) };
        }
        
        if (type == typeof(int) || type == typeof(uint))
            return "int";
        if (type == typeof(long))
            return "long";
        if (type == typeof(ulong))
            return "bytes";
        if (type == typeof(float))
            return "float";
        if (type == typeof(double) || type == typeof(decimal))
            return "double";
        if (type == typeof(bool))
            return "boolean";
        if (type == typeof(string))
            return "string";
        if (type == typeof(byte[]))
            return "bytes";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type.FullName == "System.DateOnly")
            return new Dictionary<string, object>
            {
                ["type"] = "long",
                ["logicalType"] = "timestamp-millis"
            };
        if (type == typeof(Guid))
            return new Dictionary<string, object>
            {
                ["type"] = "string",
                ["logicalType"] = "uuid"
            };
        
        if (type.IsArray)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "array",
                ["items"] = MapCSharpTypeToAvro(type.GetElementType()!, false, definedTypes)
            };
        }
        
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            if (ArrayTypes.Contains(genericTypeDef))
            {
                var itemType = type.GetGenericArguments()[0];
                return new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = MapCSharpTypeToAvro(itemType, false, definedTypes)
                };
            }

            if (MapTypes.Contains(genericTypeDef))
            {
                var keyType = type.GetGenericArguments()[0];
                if (keyType != typeof(string))
                    throw new InvalidOperationException(
                        $"Dictionary key type {keyType.Name} is not supported (only string keys are allowed)");
                
                var valueType = type.GetGenericArguments()[1];
                return new Dictionary<string, object>
                {
                    ["type"] = "map",
                    ["values"] = MapCSharpTypeToAvro(valueType, false, definedTypes)
                };
            }
        }
        
        if (type.IsEnum)
        {
            if (!definedTypes.Add($"{type.Namespace}.{type.Name}"))
                return type.Name;

            return new Dictionary<string, object>
            {
                ["type"] = "enum",
                ["name"] = type.Name,
                ["namespace"] = type.Namespace ?? string.Empty,
                ["symbols"] = Enum.GetNames(type)
            };
        }

        // Check if this is a system type or framework type that we shouldn't serialize
        var isSystemType = type.Namespace?.StartsWith("System") ?? false;
        var isMicrosoftType = type.Namespace?.StartsWith("Microsoft") ?? false;
        
        // Check if it's a delegate type (Func, Action, etc.)
        var isDelegate = typeof(Delegate).IsAssignableFrom(type);
        
        // Only generate schema for user-defined classes and value types
        var isUserDefinedClass = type.IsClass && type != typeof(string) && !isSystemType && !isMicrosoftType && !isDelegate;
        var isUserDefinedStruct = type is { IsValueType: true, IsPrimitive: false, IsEnum: false } && !isSystemType && !isMicrosoftType;
        
        if (isUserDefinedClass || isUserDefinedStruct)
        {
            if (!definedTypes.Add($"{type.Namespace}.{type.Name}"))
                return type.Name;

            return new Dictionary<string, object>
            {
                ["type"] = "record",
                ["name"] = type.Name,
                ["namespace"] = type.Namespace ?? string.Empty,
                ["fields"] = GenerateFields(type, definedTypes)
            };
        }

        throw new InvalidOperationException($"{type.Name} is not supported.");
    }
    
    private static bool IsNullable(MemberInfo member)
    {
        // Check for NullableAttribute which the C# compiler adds to nullable reference types
        var nullable = member.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
        
        if (nullable != null && nullable.ConstructorArguments.Count == 1)
        {
            var attributeArgument = nullable.ConstructorArguments[0];
            if (attributeArgument.ArgumentType == typeof(byte[]))
            {
                var args = (System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument>)attributeArgument.Value;
                if (args.Count > 0 && args[0].Value is byte byteval)
                {
                    return byteval == 2; // 2 means nullable
                }
            }
            else if (attributeArgument.ArgumentType == typeof(byte))
            {
                return (byte)attributeArgument.Value == 2;
            }
        }

        // Check the containing type's NullableContextAttribute as a fallback
        var context = member.DeclaringType?.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
        
        if (context != null && context.ConstructorArguments.Count == 1)
        {
            return (byte)context.ConstructorArguments[0].Value == 2;
        }

        return false;
    }
}