using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Kafka.Autofill;

internal static class PropertyValidator
{
    public static IEnumerable<(IPropertySymbol property, string reason)> ValidateProperties(
        IEnumerable<IPropertySymbol> properties)
    {
        var unsupportedProperties = new List<(IPropertySymbol property, string reason)>();
        
        foreach (var property in properties)
        {
            var reason = ValidatePropertyType(property.Type);
            if (reason != null)
            {
                unsupportedProperties.Add((property, reason));
            }
        }

        return unsupportedProperties;
    }

    private static string? ValidatePropertyType(ITypeSymbol type)
    {
        // Handle nullable value types
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            return ValidatePropertyType(namedType.TypeArguments[0]);
        }

        // Check for supported primitive types
        if (IsSupportedPrimitiveType(type))
            return null;

        // Arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            return ValidatePropertyType(arrayType.ElementType);
        }

        // Generic collections
        if (type is INamedTypeSymbol genericType && genericType.IsGenericType)
        {
            var genericDef = genericType.OriginalDefinition.ToDisplayString();
            
            // Supported collection types
            if (genericDef.StartsWith("System.Collections.Generic.List<") ||
                genericDef.StartsWith("System.Collections.Generic.HashSet<") ||
                genericDef.StartsWith("System.Collections.Generic.IEnumerable<") ||
                genericDef.StartsWith("System.Collections.Generic.ICollection<") ||
                genericDef.StartsWith("System.Collections.Generic.IList<"))
            {
                return ValidatePropertyType(genericType.TypeArguments[0]);
            }

            // Dictionary types
            if (genericDef.StartsWith("System.Collections.Generic.Dictionary<") ||
                genericDef.StartsWith("System.Collections.Generic.IDictionary<"))
            {
                var keyType = genericType.TypeArguments[0];
                if (keyType.SpecialType != SpecialType.System_String)
                {
                    return $"Dictionary key type '{keyType.Name}' is not supported (only string keys are allowed)";
                }
                return ValidatePropertyType(genericType.TypeArguments[1]);
            }
        }

        // Enums are supported
        if (type.TypeKind == TypeKind.Enum)
            return null;

        // Check for system/framework types that should not be serialized
        var typeNamespace = type.ContainingNamespace?.ToDisplayString();
        var isSystemType = typeNamespace?.StartsWith("System") ?? false;
        var isMicrosoftType = typeNamespace?.StartsWith("Microsoft") ?? false;
        
        // Check if it's a delegate
        if (type.TypeKind == TypeKind.Delegate)
        {
            return $"Delegate type '{type.Name}' from namespace '{typeNamespace}' is not supported in Avro schema";
        }

        // Reject system/Microsoft types that aren't already handled
        if (isSystemType || isMicrosoftType)
        {
            return $"Type '{type.Name}' from namespace '{typeNamespace}' is not supported in Avro schema";
        }

        // User-defined classes and structs - validate their properties recursively
        if (type.TypeKind == TypeKind.Class || (type.TypeKind == TypeKind.Struct && !type.IsTupleType))
        {
            var members = type.GetMembers();
            foreach (var member in members)
            {
                if (member is IPropertySymbol property && 
                    property.DeclaredAccessibility == Accessibility.Public && 
                    !property.IsStatic)
                {
                    var error = ValidatePropertyType(property.Type);
                    if (error != null)
                    {
                        return $"Property '{property.Name}' has unsupported type: {error}";
                    }
                }
            }
            return null; // User-defined type with valid properties
        }

        return $"Type '{type.Name}' from namespace '{typeNamespace}' is not supported in Avro schema";
    }

    private static bool IsSupportedPrimitiveType(ITypeSymbol type)
    {
        var specialType = type.SpecialType;
        
        // Numeric types
        if (specialType is SpecialType.System_Int32 
            or SpecialType.System_Int64 
            or SpecialType.System_UInt32 
            or SpecialType.System_UInt64
            or SpecialType.System_Single 
            or SpecialType.System_Double 
            or SpecialType.System_Decimal
            or SpecialType.System_Boolean 
            or SpecialType.System_String)
            return true;

        // Byte array
        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            return true;

        var fullTypeName = type.ToDisplayString();
        
        // Date/time types & Guid
        if (fullTypeName is "System.DateTime" or "System.DateTimeOffset" or "System.DateOnly" or "System.Guid")
            return true;

        return false;
    }
}
