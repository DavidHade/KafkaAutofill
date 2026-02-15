# Kafka.Autofill

[![NuGet](https://img.shields.io/nuget/v/Kafka.Autofill.svg)](https://www.nuget.org/packages/Kafka.Autofill/)

A C# source generator and analyzer for Apache Avro serialization in Kafka. Automatically generates `Get()` and `Put()` methods and Avro schemas for `ISpecificRecord` implementations.

## Features

**Automatic Code Generation** - Generates boilerplate serialization code  
**Live Validation** - Real-time errors for unsupported types in the IDE  
**Schema Generation** - Optionally generates Avro schemas at compile time  
**Type Safety** - Compile-time validation of property types  
**Zero Runtime Dependencies** - Only development-time dependency  

## Quick Start

### 1. Mark your class with `[KafkaAutofill]`

```csharp
using Kafka.Autofill;
using Avro.Specific;

[KafkaAutofill] // Generates Get(), Put(), and Schema
public partial class Person : ISpecificRecord
{
    public int Age { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public List<string> Hobbies { get; set; } = [];
}
```

### 2. The source generator creates

```csharp
public partial class Person
{
    private static readonly Schema Cached = Schema.Parse(AvroSchemaGen.GenerateSchema(typeof(Person)));

    [AvroIgnore]
    public Schema Schema { get; } = Cached;

    public object Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => Age,
            1 => Name,
            2 => BirthDate,
            3 => Hobbies,
            _ => throw new ArgumentOutOfRangeException(...)
        };
    }

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: Age = (int)fieldValue; break;
            case 1: Name = (string)fieldValue; break;
            case 2: BirthDate = (DateTime)fieldValue; break;
            case 3: Hobbies = (List<string>)fieldValue; break;
            default: throw new ArgumentOutOfRangeException(...);
        }
    }
}
```

## Supported Types

### Primitives
- `int`, `long`, `float`, `double`, `bool`, `string`
- `uint` (serialized as `long`)
- `ulong` (serialized as `bytes`)
- `decimal` (serialized as `double`)
- `byte[]`
- `Guid` (uuid)

### Date/Time
- `DateTime` (timestamp-millis)
- `DateTimeOffset` (timestamp-millis)
- `DateOnly` (timestamp-millis)

### Collections
- `T[]` - Arrays
- `List<T>`, `HashSet<T>`
- `IEnumerable<T>`, `ICollection<T>`, `IList<T>`
- `Dictionary<string, T>`, `IDictionary<string, T>` (string keys only)

### Complex Types
- User-defined classes and structs
- Enums
- Nullable types: `int?`, `DateTime?`, `MyEnum?`

### Nested Types

```csharp
[KafkaAutofill]
public partial class Person : ISpecificRecord
{
    public string Name { get; set; } = string.Empty;
    public Address HomeAddress { get; set; } = new();
    public List<Address> PreviousAddresses { get; set; } = [];
}

[KafkaAutofill]
public partial class Address : ISpecificRecord
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
```

## Live Type Validation

The analyzer provides **real-time errors** for unsupported types:

```csharp
[KafkaAutofill]
public partial class MyClass : ISpecificRecord
{
    // ❌ ERROR: Type 'HttpClient' from namespace 'System.Net.Http' is not supported in Avro schema
    public HttpClient Client { get; set; }
    
    // ❌ ERROR: Delegate type 'Func' from namespace 'System' is not supported in Avro schema
    public Func<string, int> Converter { get; set; }
}
```

## Excluding Properties

Use `[AvroIgnore]` to exclude properties from serialization:

```csharp
[KafkaAutofill]
public partial class Person : ISpecificRecord
{
    public string Name { get; set; } = string.Empty;
    
    [AvroIgnore]
    public Func<string, int> Converter { get; set; } // Won't be serialized
}
```

## Schema Generation Options

### Generate schema (default)
```csharp
[KafkaAutofill] // or [KafkaAutofill(true)]
public partial class Person : ISpecificRecord { }
```

### Skip schema generation
```csharp
[KafkaAutofill(false)]
public partial class Person : ISpecificRecord { }
```

## Requirements

- .NET Standard 2.1 or later
- C# 9.0 or later (for source generators)
- Apache.Avro NuGet package (for runtime)

## Contributing

Contributions are welcome! Please open an issue or pull request on [GitHub](https://github.com/DavidHade/KafkaAutofill).

## License

MIT License - see [LICENSE](LICENSE) for details.
