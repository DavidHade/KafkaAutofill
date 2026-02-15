namespace Kafka.Autofill;

/// <summary>
/// Ignores a field/property from Avro schema generation.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
internal sealed class AvroIgnoreAttribute : System.Attribute;