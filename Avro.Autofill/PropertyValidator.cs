using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Avro.Autofill;

internal static class PropertyValidator
{
    private static readonly Dictionary<ITypeSymbol, string?> ValidationCache = 
        new(SymbolEqualityComparer.Default);

    public static List<(IPropertySymbol property, string reason)> ValidateProperties(
        IEnumerable<IPropertySymbol> properties)
    {
        var unsupportedProperties = new List<(IPropertySymbol property, string reason)>();
        
        ValidationCache.Clear();
        
        foreach (var property in properties)
        {
            var reason = ValidatePropertyType(property.Type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
            if (reason != null)
            {
                unsupportedProperties.Add((property, reason));
            }
        }

        return unsupportedProperties;
    }

    private static string? ValidatePropertyType(ITypeSymbol type, HashSet<ITypeSymbol> visitedTypes)
    {
        if (ValidationCache.TryGetValue(type, out var cachedResult))
            return cachedResult;
        
        if (!visitedTypes.Add(type))
            return null;

        var result = ValidatePropertyTypeInternal(type, visitedTypes);
        ValidationCache[type] = result;
        return result;
    }

    private static string? ValidatePropertyTypeInternal(ITypeSymbol type, HashSet<ITypeSymbol> visitedTypes)
    {
        // Nullable
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            return ValidatePropertyType(namedType.TypeArguments[0], visitedTypes);
        }
        
        if (IsSupportedPrimitiveType(type))
            return null;
        
        if (type.TypeKind == TypeKind.Enum)
            return null;
        
        if (type is IArrayTypeSymbol arrayType)
        {
            return ValidatePropertyType(arrayType.ElementType, visitedTypes);
        }
        
        if (type is INamedTypeSymbol genericType && genericType.IsGenericType)
        {
            var originalDef = genericType.OriginalDefinition;
            var typeNamespace = originalDef.ContainingNamespace;
            
            if (IsSystemCollectionsGeneric(typeNamespace))
            {
                var typeName = originalDef.Name;
                
                if (typeName is "List" or "HashSet" or "IEnumerable" or "ICollection" or "IList")
                {
                    return ValidatePropertyType(genericType.TypeArguments[0], visitedTypes);
                }
                
                if (typeName is "Dictionary" or "IDictionary")
                {
                    var keyType = genericType.TypeArguments[0];
                    if (keyType.SpecialType != SpecialType.System_String)
                    {
                        return $"Dictionary key type '{keyType.Name}' is not supported (only string keys are allowed)";
                    }
                    return ValidatePropertyType(genericType.TypeArguments[1], visitedTypes);
                }
            }
        }
        
        if (type.TypeKind == TypeKind.Delegate)
        {
            var typeNamespace = GetNamespaceName(type);
            return $"Delegate type '{type.Name}' from namespace '{typeNamespace}' is not supported in Avro schema";
        }
        
        if (IsSystemOrMicrosoftType(type))
        {
            var typeNamespace = GetNamespaceName(type);
            return $"Type '{type.Name}' from namespace '{typeNamespace}' is not supported in Avro schema";
        }

        // User-defined classes and structs - validate their properties recursively
        if (type.TypeKind == TypeKind.Class || (type.TypeKind == TypeKind.Struct && !type.IsTupleType))
        {
            // Use GetMembers() with specific filter to only get properties
            var properties = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public);

            foreach (var property in properties)
            {
                var error = ValidatePropertyType(property.Type, visitedTypes);
                if (error != null)
                {
                    return $"Property '{property.Name}' has unsupported type: {error}";
                }
            }
            return null; // User-defined type with valid properties
        }

        var ns = GetNamespaceName(type);
        return $"Type '{type.Name}' from namespace '{ns}' is not supported in Avro schema";
    }

    private static bool IsSystemCollectionsGeneric(INamespaceSymbol? namespaceSymbol)
    {
        if (namespaceSymbol == null) return false;
        
        // Walk up namespace hierarchy efficiently
        if (namespaceSymbol.Name != "Generic") return false;
        namespaceSymbol = namespaceSymbol.ContainingNamespace;
        
        if (namespaceSymbol.Name != "Collections") return false;
        namespaceSymbol = namespaceSymbol.ContainingNamespace;
        
        if (namespaceSymbol.Name != "System") return false;
        
        return namespaceSymbol.ContainingNamespace?.IsGlobalNamespace ?? false;
    }

    private static bool IsSystemOrMicrosoftType(ITypeSymbol type)
    {
        var ns = type.ContainingNamespace;
        while (ns != null && !ns.IsGlobalNamespace)
        {
            var name = ns.Name;
            if (name == "System" || name == "Microsoft")
                return true;
            ns = ns.ContainingNamespace;
        }
        return false;
    }

    private static string GetNamespaceName(ITypeSymbol type)
    {
        return type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
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
