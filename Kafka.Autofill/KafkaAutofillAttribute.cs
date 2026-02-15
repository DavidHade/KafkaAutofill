namespace Kafka.Autofill;

#pragma warning disable CS9113 // Parameter is unread.
/// <summary>
/// <para>Generates a full implementation of ISpecificRecord:
/// Schema, Get(...), Put(...).</para>
/// Most common types supported,
/// check <see href="https://github.com/DavidHade/KafkaAutofill?tab=readme-ov-file#supported-types">Supported Types</see>
/// for a full list of supported property types
/// </summary>
/// <param name="generateSchema">
/// If false, you have to implement the Schema property yourself.
/// </param>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)]
internal sealed class KafkaAutofillAttribute(bool generateSchema = true) : System.Attribute;
#pragma warning restore CS9113 // Parameter is unread.