using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kafka.Autofill;

[Generator]
internal class KafkaAutofill : IIncrementalGenerator
{
    private const string KafkaAutofillAttribute = "Kafka.Autofill.KafkaAutofillAttribute";
    private static readonly string AvroGenSrc = LoadResource("Kafka.Autofill.AvroSchemaGen.cs");
    private static readonly string AvroIgnoreSrc = LoadResource("Kafka.Autofill.AvroIgnoreAttribute.cs");
    private static readonly string KafkaAutofillSrc = LoadResource($"{KafkaAutofillAttribute}.cs");
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(RegisterPostInitializationOutput);

        // Create a pipeline that finds all classes with attributes
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // Combine the classes with the compilation
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the source code
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    private static ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Check if any attribute is KafkaAutofill
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;
                if (symbol is IMethodSymbol attributeSymbol)
                {
                    var attributeClass = attributeSymbol.ContainingType;
                    if (attributeClass.ToDisplayString() == KafkaAutofillAttribute)
                    {
                        return classDeclaration;
                    }
                }
            }
        }

        return null!;
    }

    private static void Execute(
        Compilation compilation, 
        ImmutableArray<ClassDeclarationSyntax> classes, 
        SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        var attributeSymbol = compilation.GetTypeByMetadataName(KafkaAutofillAttribute);
        if (attributeSymbol is null)
            return;

        foreach (var classDeclaration in classes.Distinct())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            
            if (classSymbol is null)
                continue;

            var kafkaAttribute = classSymbol.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);
            
            if (kafkaAttribute is null)
                continue;

            var generateSchema = kafkaAttribute.ConstructorArguments.Length > 0
                                 && kafkaAttribute.ConstructorArguments[0].Value is true;
            
            var properties = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && 
                            !p.IsStatic && 
                            !p.IsIndexer &&
                            p.Name != "Schema") // Exclude Schema property
                .ToList();
            
            if (!ValidateTargets(context, properties, classDeclaration)) 
                continue; // Skip code generation for this class

            var source = GenerateClass(classSymbol, properties, generateSchema);
            context.AddSource($"{classDeclaration.Identifier}_Generated.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static bool ValidateTargets(
        SourceProductionContext context, 
        List<IPropertySymbol> properties,
        ClassDeclarationSyntax classDeclaration)
    {
        var unsupportedProperties = PropertyValidator.ValidateProperties(properties).ToList();

        if (!unsupportedProperties.Any())
            return true;

        foreach (var (property, reason) in unsupportedProperties)
        {
            var diagnostic = Diagnostic.Create(
                KafkaAutofillAnalyzer.UnsupportedTypeError,
                property.Locations.FirstOrDefault() ?? classDeclaration.GetLocation(),
                reason);
            context.ReportDiagnostic(diagnostic);
        }

        return false;
    }

    private static void RegisterPostInitializationOutput(IncrementalGeneratorPostInitializationContext postContext)
    {
        postContext.AddSource("KafkaAutofillAttribute.g.cs", SourceText.From(KafkaAutofillSrc, Encoding.UTF8));
        postContext.AddSource("AvroIgnoreAttribute.g.cs", SourceText.From(AvroIgnoreSrc, Encoding.UTF8));
        postContext.AddSource("AvroSchemaGen.g.cs", SourceText.From(AvroGenSrc, Encoding.UTF8));
    }

    private static string GenerateClass(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, bool generateSchema)
    {
        var sb = new StringBuilder();
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        HashSet<string> namespaces = [];
        _ = namespaces.Add("System");

        const string outOfRage = "throw new ArgumentOutOfRangeException(nameof(fieldPos), fieldPos, \"Invalid field position.\")";
        const string usings = 
            """
            // <auto-generated />
            using Avro;
            using Kafka.Autofill;
            
            """;
        
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {className}");
        sb.AppendLine("{");
        
        if (generateSchema)
        {
            sb.AppendLine($"    private static readonly Schema Cached = Schema.Parse(AvroSchemaGen.GenerateSchema(typeof({className})));");
            sb.AppendLine();
            sb.AppendLine("    [AvroIgnore]");
            sb.AppendLine($"    public Schema Schema {{ get; }} = Cached;");
        }

        sb.AppendLine();
        sb.AppendLine("    public object Get(int fieldPos)");
        sb.AppendLine("    {");
        sb.AppendLine("        return fieldPos switch");
        sb.AppendLine("        {");
        
        for (var i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var getValue = GenerateGetConversion(prop.Type, prop.Name);
            sb.AppendLine($"            {i} => {getValue},");
        }
        
        sb.AppendLine($"            _ => {outOfRage}");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("#nullable enable");
        sb.AppendLine("    public void Put(int fieldPos, object fieldValue)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (fieldPos)");
        sb.AppendLine("        {");
        
        for (var i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var conversion = GeneratePutConversion(prop.Type, "fieldValue", namespaces);
            sb.AppendLine($"            case {i}: {prop.Name} = {conversion}; break;");
        }
        
        sb.AppendLine($"            default: {outOfRage};");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("#nullable restore");

        // Close the class and namespace
        sb.AppendLine("}");

        var ns = string.Join(Environment.NewLine, namespaces.Select(ns => $"using {ns};"));
        var rest = sb.ToString();

        return usings + ns + rest;
    }

    private static string GenerateGetConversion(ITypeSymbol type, string propertyName)
    {
        var specialType = type.SpecialType;

        // Handle nullable value types
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            var underlyingType = namedType.TypeArguments[0];
            var underlyingSpecialType = underlyingType.SpecialType;

            // Nullable unsigned types
            if (underlyingSpecialType == SpecialType.System_UInt32)
                return $"{propertyName}.HasValue ? (long){propertyName}.Value : (long?)null";
            if (underlyingSpecialType == SpecialType.System_UInt64)
                return $"{propertyName}.HasValue ? System.BitConverter.GetBytes({propertyName}.Value) : (byte[]?)null";

            // Other nullable types (int?, double?, bool?, etc.)
            return propertyName;
        }

        // Decimal -> cast to double
        if (specialType == SpecialType.System_Decimal)
            return $"(double){propertyName}";
        
        // Unsigned types -> cast to int/long or bytes
        if (specialType == SpecialType.System_UInt16)
            return $"(int){propertyName}";
        if (specialType == SpecialType.System_UInt32)
            return $"(long){propertyName}";
        if (specialType == SpecialType.System_UInt64)
            return $"BitConverter.GetBytes({propertyName})";

        // Collections that need conversion
        if (type is INamedTypeSymbol genericType && genericType.IsGenericType)
        {
            var genericDef = genericType.OriginalDefinition.ToDisplayString();

            // IEnumerable/ICollection/IList (not List) -> convert to List
            if (genericDef.StartsWith("System.Collections.Generic.HashSet<") ||
                genericDef.StartsWith("System.Collections.Generic.IEnumerable<") ||
                genericDef.StartsWith("System.Collections.Generic.ICollection<"))
                return $"{propertyName}.ToList()";
        }

        // All other types return as-is
        return propertyName;
    }

    private static string GeneratePutConversion(ITypeSymbol type, string expr, HashSet<string> namespaces)
    {
        // Handle nullable value types (e.g., int?, Guid?, DateTime?)
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            var underlyingType = namedType.TypeArguments[0];
            var underlyingTypeName = underlyingType.ToDisplayString();
            
            // Nullable DateTime from long timestamp
            if (underlyingTypeName == "System.DateTime")
                return $"(DateTime?){expr}";

            if (underlyingTypeName == "System.DateTimeOffset")
                return $"(DateTimeOffset?){expr}";
            
            // Nullable Guid from string
            if (underlyingTypeName == "System.Guid")
                return $"(Guid?){expr}";

            // Nullable enums from string
            if (underlyingType.TypeKind == TypeKind.Enum)
                return $"{expr} == null ? ({underlyingType.Name}?)null : ({underlyingType.Name}){expr}";
            
            // Nullable ulong from bytes
            if (underlyingType.SpecialType == SpecialType.System_UInt64)
                return $"{expr} == null ? (ulong?)null : BitConverter.ToUInt64((byte[]){expr}, 0)";

            // Other nullable value types (int?, double?, bool?, etc.)
            return $"({underlyingType.Name}?){expr}";
        }
        
        // Handle nullable reference types (e.g., string?, List<string>?, Address?)
        if (type.NullableAnnotation == NullableAnnotation.Annotated && !type.IsValueType)
        {
            namespaces.Add(type.ContainingNamespace.ToDisplayString());
            return $"({type.Name}){expr}";
        }
        
        return GeneratePutConversionForNonNullableType(type, expr, namespaces);
    }
    
    private static string GeneratePutConversionForNonNullableType(ITypeSymbol type, string expr, HashSet<string> namespaces)
    {
        var fullTypeName = type.ToDisplayString();
        var specialType = type.SpecialType;

        // Unsigned types - convert from long/bytes
        if (specialType == SpecialType.System_UInt32)
        {
            namespaces.Add("System");
            return $"(uint)Convert.ToInt64({expr})";
        }

        if (specialType == SpecialType.System_UInt64)
        {
            namespaces.Add("System");
            return $"BitConverter.ToUInt64((byte[]){expr}, 0)";
        }
        
        // Decimal - Avro equivalent type is double
        if (specialType == SpecialType.System_Decimal)
            return $"(decimal)(double){expr}";
        
        // DateTime from long timestamp-millis
        if (fullTypeName == "System.DateTime")
        {
            namespaces.Add("System");
            return $"(DateTime){expr}";
        }

        if (fullTypeName == "System.DateTimeOffset")
        {
            namespaces.Add("System");
            return $"(DateTimeOffset){expr}";
        }
        
        // Guid from string
        if (fullTypeName == "System.Guid")
        {
            namespaces.Add("System");
            return $"(Guid){expr}";
        }
        
        // Byte array
        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            return $"(byte[]){expr}";
        
        // Arrays - Avro deserializes as IList or object[], cast to array type
        if (type is IArrayTypeSymbol)
        {
            return $"({fullTypeName}){expr}";
        }
        
        // Generic types (collections, dictionaries)
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var genericDef = namedType.OriginalDefinition.ToDisplayString();
            
            // HashSet - Avro returns IEnumerable, cast and wrap
            if (genericDef.StartsWith("System.Collections.Generic.HashSet<"))
            {
                var elementType = namedType.TypeArguments[0].Name;
                namespaces.Add("System.Collections.Generic");
                return $"new HashSet<{elementType}>((IEnumerable<{elementType}>){expr})";
            }
            
            // List - direct cast
            if (genericDef.StartsWith("System.Collections.Generic.List<"))
            {
                namespaces.Add("System.Collections.Generic");
                var elementType = namedType.TypeArguments[0].Name;
                return $"({type.Name}<{elementType}>){expr}";
            }
            
            // IEnumerable, ICollection, IList - direct cast
            if (genericDef.StartsWith("System.Collections.Generic.IEnumerable<") ||
                genericDef.StartsWith("System.Collections.Generic.ICollection<") ||
                genericDef.StartsWith("System.Collections.Generic.IList<"))
            {
                namespaces.Add("System.Collections.Generic");
                var elementType = namedType.TypeArguments[0].Name;
                return $"({type.Name}<{elementType}>){expr}";
            }
            
            // Dictionary types - direct cast
            if (genericDef.StartsWith("System.Collections.Generic.Dictionary<") ||
                genericDef.StartsWith("System.Collections.Generic.IDictionary<"))
            {
                namespaces.Add("System.Collections.Generic");
                var key = namedType.TypeArguments[0].Name;
                var val = namedType.TypeArguments[1].Name;
                return $"({type.Name}<{key}, {val}>){expr}";
            }
        }
        
        // Reference types (classes, structs, records) - direct cast
        namespaces.Add(type.ContainingNamespace.ToDisplayString());
        return $"({type.Name}){expr}";
    }

    private static string LoadResource(string name)
    {
        var assembly = typeof(KafkaAutofill).Assembly;
        
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
            throw new FileNotFoundException($"Cannot find resource {name}");
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}