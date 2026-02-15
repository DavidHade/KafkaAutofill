namespace Kafka.Autofill;

#pragma warning disable CS9113 // Parameter is unread.
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)]
public sealed class KafkaAutofillAttribute(bool generateSchema = true) : System.Attribute;
#pragma warning restore CS9113 // Parameter is unread.