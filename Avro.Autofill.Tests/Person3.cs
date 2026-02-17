// ReSharper disable MemberCanBePrivate.Global
using Avro.Specific;

namespace Avro.Autofill.Tests.Person3;

[AvroAutofill(false)]
// Only used in Benchmarks
public partial class Person3 : BasePerson, ISpecificRecord
{
    private static readonly Schema Cached = Schema.Parse(AvroSchemaGen.GenerateSchema(typeof(Person3)));
    
    [AvroIgnore] 
    public Schema Schema { get; set; } = Cached;
    
}