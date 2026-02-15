using Avro;
using Avro.Specific;

namespace Kafka.Autofill.Tests.Person3;

[KafkaAutofill(false)]
public partial class Person3 : ISpecificRecord
{
    private static readonly Schema Cached = Schema.Parse(AvroSchemaGen.GenerateSchema(typeof(Person3)));
    
    [AvroIgnore] 
    public Schema Schema { get; set; } = Cached;
    
    // Primitive types
    public int Age { get; set; }
    public uint UnsignedAge { get; set; }
    public long LongValue { get; set; }
    public ulong UnsignedLongValue { get; set; }
    public float Height { get; set; }
    public double Weight { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[] ProfilePicture { get; set; } = [];
    
    // DateTime types
    public DateTime BirthDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    // Guid
    public Guid Id { get; set; }
    
    // Nullable primitives
    public int? OptionalAge { get; set; }
    public double? OptionalWeight { get; set; }
    public decimal? OptionalSalary { get; set; }
    public bool? OptionalFlag { get; set; }
    public DateTime? OptionalDate { get; set; }
    public Guid? OptionalId { get; set; }
    
    // Arrays
    public int[] Scores { get; set; } = [];
    public string[] Tags { get; set; } = [];
    
    // Collections
    public List<string> Hobbies { get; set; } = [];
    public HashSet<string> Skills { get; set; } = [];
    public IEnumerable<int> Ratings { get; set; } = [];
    public ICollection<string> Achievements { get; set; } = [];
    public IList<double> Measurements { get; set; } = [];
    
    // Dictionaries
    public Dictionary<string, string> Metadata { get; set; } = [];
    public IDictionary<string, int> Stats { get; set; } = new Dictionary<string, int>();
    
    // Enum
    public GenderEnum Gender { get; set; }
    public GenderEnum? OptionalGender { get; set; }
    
    // Nested records
    public Address HomeAddress { get; set; } =  new();
    public Address? OfficeAddress { get; set; }
    
    // Complex collections
    public List<Address> PreviousAddresses { get; set; } = [];
    public Dictionary<string, Address> AddressBook { get; set; } = [];
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public enum GenderEnum
{
    Male,
    Female,
    Other
}