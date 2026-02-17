// ReSharper disable MemberCanBePrivate.Global
using Avro.Specific;

namespace Avro.Autofill.Tests.Person2;

[AvroAutofill(false)]
public partial class Person2 : BasePerson, ISpecificRecord
{
    [AvroIgnore]
    public Schema Schema { get; set; } = Schema.Parse(AvroSchemaGen.GenerateSchema(typeof(Person2)));
}