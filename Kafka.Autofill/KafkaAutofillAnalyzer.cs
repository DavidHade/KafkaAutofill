using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kafka.Autofill;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class KafkaAutofillAnalyzer : DiagnosticAnalyzer
{
    private const string KafkaAutofillAttribute = "Kafka.Autofill.KafkaAutofillAttribute";

    internal static readonly DiagnosticDescriptor UnsupportedTypeError = new(
        id: "KAFKA001",
        title: "Unsupported type in Avro schema",
        messageFormat: "{0}",
        category: "Kafka.Autofill",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "This type cannot be serialized with Apache Avro.",
        helpLinkUri: "https://avro.apache.org/docs/1.12.0/specification/");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(UnsupportedTypeError);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for symbol analysis
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        // Check if the class has the KafkaAutofill attribute
        var hasKafkaAutofillAttribute = namedTypeSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == KafkaAutofillAttribute);

        if (!hasKafkaAutofillAttribute)
            return;

        // Get all public properties
        var properties = namedTypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && 
                        !p.IsStatic && 
                        !p.IsIndexer &&
                        p.Name != "Schema")
            .ToList();

        // Validate properties using shared logic
        var unsupportedProperties = PropertyValidator.ValidateProperties(properties);

        // Report diagnostics for each unsupported property
        foreach (var (property, reason) in unsupportedProperties)
        {
            var location = property.Locations.FirstOrDefault();
            if (location != null)
            {
                var diagnostic = Diagnostic.Create(
                    UnsupportedTypeError,
                    location,
                    reason);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
