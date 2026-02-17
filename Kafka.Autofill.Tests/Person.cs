using Avro.Specific;

namespace Kafka.Autofill.Tests;

[KafkaAutofill]
public partial class Person : BasePerson, ISpecificRecord
{
    
}