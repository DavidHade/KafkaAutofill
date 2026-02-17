// ReSharper disable MemberCanBePrivate.Global
using Avro;
using Avro.Specific;

namespace Kafka.Autofill.Tests.Person3;

[KafkaAutofill(false)]
// Only used in Benchmarks
public partial class Person3 : BasePerson, ISpecificRecord
{
    private static readonly Schema Cached = Schema.Parse(AvroSchemaGen.GenerateSchema(typeof(Person3)));
    
    [AvroIgnore] 
    public Schema Schema { get; set; } = Cached;
    
}