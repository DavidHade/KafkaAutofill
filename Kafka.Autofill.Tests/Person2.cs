// ReSharper disable MemberCanBePrivate.Global
using Avro;
using Avro.Specific;

namespace Kafka.Autofill.Tests.Person2;

[KafkaAutofill(false)]
public partial class Person2 : BasePerson, ISpecificRecord
{
    [AvroIgnore]
    public Schema Schema { get; set; } = Schema.Parse(AvroSchemaGen.GenerateSchema(typeof(Person2)));
}