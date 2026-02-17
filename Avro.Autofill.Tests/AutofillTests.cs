using Avro;

namespace Avro.Autofill.Tests;

public class AutofillTests
{
    [Fact]
    public void Person_Schema_Should_Have_Correct_Name_And_Namespace()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;

        Assert.NotNull(schema);
        Assert.Equal("Person", schema.Name);
        Assert.Equal("Avro.Autofill.Tests", schema.Namespace);
    }

    [Fact]
    public void Person_Schema_Should_Have_All_Properties_As_Fields()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;

        Assert.NotNull(schema);

        // Count the number of public properties in Person (excluding Schema itself)
        var propertyCount = typeof(Person).GetProperties().Length - 1; // -1 for Schema property
        Assert.Equal(propertyCount, schema.Count);
    }

    [Fact]
    public void Person_Put_Should_Throw_For_Invalid_Position()
    {
        var person = new Person();
        var propertyCount = typeof(Person).GetProperties().Length - 1;
        
        Assert.Throws<ArgumentOutOfRangeException>(() => person.Put(-1, 42));
        Assert.Throws<ArgumentOutOfRangeException>(() => person.Put(propertyCount + 10, 42));
    }

    [Fact]
    public void Person_Get_Should_Throw_For_Invalid_Position()
    {
        var person = new Person();
        var propertyCount = typeof(Person).GetProperties().Length - 1;
        
        Assert.Throws<ArgumentOutOfRangeException>(() => person.Get(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => person.Get(propertyCount + 10));
    }

    [Fact]
    public void Person_Put_Should_Set_Complex_Properties()
    {
        var person = new Person();
        var address = new Address { Street = "123 Test St", City = "TestCity", ZipCode = "12345" };
        var hobbies = new List<string> { "swimming", "running" };
        var metadata = new Dictionary<string, string> { { "key", "value" } };

        // Find the field positions by inspecting the schema
        var schema = person.Schema as RecordSchema;
        var hobbyField = schema?.Fields.FirstOrDefault(f => f.Name == "Hobbies");
        var homeAddressField = schema?.Fields.FirstOrDefault(f => f.Name == "HomeAddress");
        var metadataField = schema?.Fields.FirstOrDefault(f => f.Name == "Metadata");

        if (hobbyField != null && homeAddressField != null && metadataField != null)
        {
            // Act
            person.Put(hobbyField.Pos, hobbies);
            person.Put(homeAddressField.Pos, address);
            person.Put(metadataField.Pos, metadata);

            Assert.Equal(hobbies, person.Hobbies);
            Assert.Equal(address, person.HomeAddress);
            Assert.Equal(metadata, person.Metadata);
        }
    }

    [Fact]
    public void Person_Schema_Should_Include_All_Properties()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;

        Assert.NotNull(schema);

        // Check that schema includes fields for all primitive types
        Assert.Contains(schema.Fields, f => f.Name == "Age");
        Assert.Contains(schema.Fields, f => f.Name == "UnsignedAge");
        Assert.Contains(schema.Fields, f => f.Name == "LongValue");
        Assert.Contains(schema.Fields, f => f.Name == "Height");
        Assert.Contains(schema.Fields, f => f.Name == "Weight");
        Assert.Contains(schema.Fields, f => f.Name == "Salary");
        Assert.Contains(schema.Fields, f => f.Name == "IsActive");
        Assert.Contains(schema.Fields, f => f.Name == "Name");
        Assert.Contains(schema.Fields, f => f.Name == "ProfilePicture");

        // Dates & Guid
        Assert.Contains(schema.Fields, f => f.Name == "DateOfBirth");
        Assert.Contains(schema.Fields, f => f.Name == "OptionalDateOfBirthNonNull");
        Assert.Contains(schema.Fields, f => f.Name == "OptionalDateOfBirthNull");
        Assert.Contains(schema.Fields, f => f.Name == "BirthDate");
        Assert.Contains(schema.Fields, f => f.Name == "CreatedAt");
        Assert.Contains(schema.Fields, f => f.Name == "Id");

        // Nullable
        Assert.Contains(schema.Fields, f => f.Name == "OptionalAge");
        Assert.Contains(schema.Fields, f => f.Name == "OptionalWeight");
        Assert.Contains(schema.Fields, f => f.Name == "OptionalFlag");
        Assert.Contains(schema.Fields, f => f.Name == "OptionalDate");
        Assert.Contains(schema.Fields, f => f.Name == "OptionalId");
        Assert.Contains(schema.Fields, f => f.Name == "OptionalGender");

        // Collections
        Assert.Contains(schema.Fields, f => f.Name == "Scores");
        Assert.Contains(schema.Fields, f => f.Name == "Tags");
        Assert.Contains(schema.Fields, f => f.Name == "Hobbies");
        Assert.Contains(schema.Fields, f => f.Name == "Skills");
        Assert.Contains(schema.Fields, f => f.Name == "Ratings");
        Assert.Contains(schema.Fields, f => f.Name == "Achievements");
        Assert.Contains(schema.Fields, f => f.Name == "Measurements");

        // Dictionaries
        Assert.Contains(schema.Fields, f => f.Name == "Metadata");
        Assert.Contains(schema.Fields, f => f.Name == "Stats");

        // Enums
        Assert.Contains(schema.Fields, f => f.Name == "Gender");

        // Nested types
        Assert.Contains(schema.Fields, f => f.Name == "HomeAddress");
        Assert.Contains(schema.Fields, f => f.Name == "OfficeAddress");
        Assert.Contains(schema.Fields, f => f.Name == "PreviousAddresses");
        Assert.Contains(schema.Fields, f => f.Name == "AddressBook");
    }
    
    [Fact]
    public void Person_Get_And_Put_Should_Be_Reversible()
    {
        var person = TestData.Person;

        var schema = person.Schema as RecordSchema;

        Assert.NotNull(schema);

        foreach (var field in schema.Fields)
        {
            if (field.Name == nameof(Person.Schema)) continue;
            var prop = typeof(Person).GetProperty(field.Name);

            if (prop is null)
                Assert.Fail($"Property {field.Name} not found on type {person.GetType()}");

            var expected = prop.GetValue(person);
            person.Put(field.Pos, person.Get(field.Pos)!);
            var actual = prop.GetValue(person);

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Person_Schema_Age_Field_Should_Be_Int()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var ageField = schema?.Fields.FirstOrDefault(f => f.Name == "Age");

        Assert.NotNull(ageField);
        Assert.Equal(Schema.Type.Int, ageField.Schema.Tag);
    }

    [Fact]
    public void Person_Schema_LongValue_Field_Should_Be_Long()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var longField = schema?.Fields.FirstOrDefault(f => f.Name == "LongValue");

        Assert.NotNull(longField);
        Assert.Equal(Schema.Type.Long, longField.Schema.Tag);
    }

    [Fact]
    public void Person_Schema_Height_Field_Should_Be_Float()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var heightField = schema?.Fields.FirstOrDefault(f => f.Name == "Height");

        Assert.NotNull(heightField);
        Assert.Equal(Schema.Type.Float, heightField.Schema.Tag);
    }

    [Fact]
    public void Person_Schema_Weight_Field_Should_Be_Double()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var weightField = schema?.Fields.FirstOrDefault(f => f.Name == "Weight");

        Assert.NotNull(weightField);
        Assert.Equal(Schema.Type.Double, weightField.Schema.Tag);
    }

    [Fact]
    public void Person_Schema_IsActive_Field_Should_Be_Boolean()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var isActiveField = schema?.Fields.FirstOrDefault(f => f.Name == "IsActive");

        Assert.NotNull(isActiveField);
        Assert.Equal(Schema.Type.Boolean, isActiveField.Schema.Tag);
    }

    [Fact]
    public void Person_Schema_Name_Field_Should_Be_String()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var nameField = schema?.Fields.FirstOrDefault(f => f.Name == "Name");

        Assert.NotNull(nameField);
        Assert.Equal(Schema.Type.String, nameField.Schema.Tag);
    }

    [Fact]
    public void Person_Schema_ProfilePicture_Field_Should_Be_Bytes()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var pictureField = schema?.Fields.FirstOrDefault(f => f.Name == "ProfilePicture");

        Assert.NotNull(pictureField);
        Assert.Equal(Schema.Type.Bytes, pictureField.Schema.Tag);
    }

    [Fact]
    public void Person_Schema_BirthDate_Field_Should_Be_Long_With_TimestampMillis_LogicalType()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var birthDateField = schema?.Fields.FirstOrDefault(f => f.Name == "BirthDate");

        Assert.NotNull(birthDateField);
        var logicalSchema = birthDateField.Schema as LogicalSchema;
        Assert.NotNull(logicalSchema);
        Assert.Equal("timestamp-millis", logicalSchema.LogicalTypeName);
    }

    [Fact]
    public void Person_Schema_Id_Field_Should_Be_String_With_Uuid_LogicalType()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var idField = schema?.Fields.FirstOrDefault(f => f.Name == "Id");

        Assert.NotNull(idField);
        var logicalSchema = idField.Schema as LogicalSchema;
        Assert.NotNull(logicalSchema);
        Assert.Equal("uuid", logicalSchema.LogicalTypeName);
    }

    [Fact]
    public void Person_Schema_OptionalAge_Field_Should_Be_Union_With_Null()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var optionalAgeField = schema?.Fields.FirstOrDefault(f => f.Name == "OptionalAge");

        Assert.NotNull(optionalAgeField);
        var unionSchema = optionalAgeField.Schema as UnionSchema;
        Assert.NotNull(unionSchema);
        Assert.Equal(2, unionSchema.Count);
        Assert.Contains(unionSchema.Schemas, s => s.Tag == Schema.Type.Null);
        Assert.Contains(unionSchema.Schemas, s => s.Tag == Schema.Type.Int);
    }

    [Fact]
    public void Person_Schema_Scores_Field_Should_Be_Array_Of_Int()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var scoresField = schema?.Fields.FirstOrDefault(f => f.Name == "Scores");

        Assert.NotNull(scoresField);
        var arraySchema = scoresField.Schema as ArraySchema;
        Assert.NotNull(arraySchema);
        Assert.Equal(Schema.Type.Int, arraySchema.ItemSchema.Tag);
    }

    [Fact]
    public void Person_Schema_Hobbies_Field_Should_Be_Array_Of_String()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var hobbiesField = schema?.Fields.FirstOrDefault(f => f.Name == "Hobbies");

        Assert.NotNull(hobbiesField);
        var arraySchema = hobbiesField.Schema as ArraySchema;
        Assert.NotNull(arraySchema);
        Assert.Equal(Schema.Type.String, arraySchema.ItemSchema.Tag);
    }

    [Fact]
    public void Person_Schema_Metadata_Field_Should_Be_Map_Of_String()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var metadataField = schema?.Fields.FirstOrDefault(f => f.Name == "Metadata");

        Assert.NotNull(metadataField);
        var mapSchema = metadataField.Schema as MapSchema;
        Assert.NotNull(mapSchema);
        Assert.Equal(Schema.Type.String, mapSchema.ValueSchema.Tag);
    }

    [Fact]
    public void Person_Schema_Stats_Field_Should_Be_Map_Of_Int()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var statsField = schema?.Fields.FirstOrDefault(f => f.Name == "Stats");

        Assert.NotNull(statsField);
        var mapSchema = statsField.Schema as MapSchema;
        Assert.NotNull(mapSchema);
        Assert.Equal(Schema.Type.Int, mapSchema.ValueSchema.Tag);
    }

    [Fact]
    public void Person_Schema_Gender_Field_Should_Be_Enum()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var genderField = schema?.Fields.FirstOrDefault(f => f.Name == "Gender");

        Assert.NotNull(genderField);
        var enumSchema = genderField.Schema as EnumSchema;
        Assert.NotNull(enumSchema);
        Assert.Equal("GenderEnum", enumSchema.Name);
        Assert.Contains("Male", enumSchema.Symbols);
        Assert.Contains("Female", enumSchema.Symbols);
    }

    [Fact]
    public void Person_Schema_HomeAddress_Field_Should_Be_Record()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var homeAddressField = schema?.Fields.FirstOrDefault(f => f.Name == "HomeAddress");

        Assert.NotNull(homeAddressField);
        var recordSchema = homeAddressField.Schema as RecordSchema;
        Assert.NotNull(recordSchema);
        Assert.Equal("Address", recordSchema.Name);
        Assert.Contains(recordSchema.Fields, f => f.Name == "Street");
        Assert.Contains(recordSchema.Fields, f => f.Name == "City");
        Assert.Contains(recordSchema.Fields, f => f.Name == "ZipCode");
    }

    [Fact]
    public void Person_Schema_PreviousAddresses_Field_Should_Be_Array_Of_Address_Records()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var previousAddressesField = schema?.Fields.FirstOrDefault(f => f.Name == "PreviousAddresses");
        
        Assert.NotNull(previousAddressesField);
        var arraySchema = previousAddressesField.Schema as ArraySchema;
        Assert.NotNull(arraySchema);
        var recordSchema = arraySchema.ItemSchema as RecordSchema;
        Assert.NotNull(recordSchema);
        Assert.Equal("Address", recordSchema.Name);
    }

    [Fact]
    public void Person_Schema_AddressBook_Field_Should_Be_Map_Of_Address_Records()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        var addressBookField = schema?.Fields.FirstOrDefault(f => f.Name == "AddressBook");
        
        Assert.NotNull(addressBookField);
        var mapSchema = addressBookField.Schema as MapSchema;
        Assert.NotNull(mapSchema);
        var recordSchema = mapSchema.ValueSchema as RecordSchema;
        Assert.NotNull(recordSchema);
        Assert.Equal("Address", recordSchema.Name);
    }

    [Fact]
    public void Person_Get_Should_Return_Correct_Values_For_All_Fields()
    {
        var birthDate = new DateTime(1994, 1, 15);
        var person = TestData.Person;
        person.DateOfBirth = DateOnly.FromDateTime(birthDate);
        person.OptionalDateOfBirthNull = null;

        var schema = person.Schema as RecordSchema;
        Assert.NotNull(schema);
        
        var ageField = schema.Fields.FirstOrDefault(f => f.Name == "Age");
        Assert.NotNull(ageField);
        Assert.Equal(30, person.Get(ageField.Pos));

        var nameField = schema.Fields.FirstOrDefault(f => f.Name == "Name");
        Assert.NotNull(nameField);
        Assert.Equal("John Doe", person.Get(nameField.Pos));

        var heightField = schema.Fields.FirstOrDefault(f => f.Name == "Height");
        Assert.NotNull(heightField);
        Assert.Equal(5.9f, person.Get(heightField.Pos));

        var isActiveField = schema.Fields.FirstOrDefault(f => f.Name == "IsActive");
        Assert.NotNull(isActiveField);
        Assert.Equal(true, person.Get(isActiveField.Pos));

        var birthDateField = schema.Fields.FirstOrDefault(f => f.Name == "BirthDate");
        Assert.NotNull(birthDateField);
        Assert.Equal(birthDate, person.Get(birthDateField.Pos));
        
        var dateOfBirthField = schema.Fields.FirstOrDefault(f => f.Name == "DateOfBirth");
        Assert.NotNull(dateOfBirthField);
        Assert.Equal(birthDate, person.Get(dateOfBirthField.Pos));
        
        var dateOfBirthOptionalField = schema.Fields.FirstOrDefault(f => f.Name == "OptionalDateOfBirthNull");
        Assert.NotNull(dateOfBirthOptionalField);
        Assert.Null(person.Get(dateOfBirthOptionalField.Pos));

        var idField = schema.Fields.FirstOrDefault(f => f.Name == "Id");
        Assert.NotNull(idField);
        Assert.Equal(Guid.Parse("B381A45F-0656-4452-80ED-D734C05C6329"), person.Get(idField.Pos));
    }

    [Fact]
    public void Person_Put_Should_Set_All_Field_Types_Correctly()
    {
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        Assert.NotNull(schema);

        var guid = Guid.NewGuid();
        var birthDate = new DateTime(1985, 3, 20);
        var address = new Address { Street = "Test St", City = "Test City", ZipCode = "00000" };
        var hobbies = new List<string> { "music", "sports" };
        var metadata = new Dictionary<string, string> { { "foo", "bar" } };
        
        var ageField = schema.Fields.FirstOrDefault(f => f.Name == "Age");
        person.Put(ageField!.Pos, 40);
        Assert.Equal(40, person.Age);

        var nameField = schema.Fields.FirstOrDefault(f => f.Name == "Name");
        person.Put(nameField!.Pos, "Bob");
        Assert.Equal("Bob", person.Name);

        var isActiveField = schema.Fields.FirstOrDefault(f => f.Name == "IsActive");
        person.Put(isActiveField!.Pos, true);
        Assert.True(person.IsActive);

        var idField = schema.Fields.FirstOrDefault(f => f.Name == "Id");
        person.Put(idField!.Pos, guid);
        Assert.Equal(guid, person.Id);

        var birthDateField = schema.Fields.FirstOrDefault(f => f.Name == "BirthDate");
        person.Put(birthDateField!.Pos, birthDate);
        Assert.Equal(birthDate, person.BirthDate);

        var homeAddressField = schema.Fields.FirstOrDefault(f => f.Name == "HomeAddress");
        person.Put(homeAddressField!.Pos, address);
        Assert.Equal(address, person.HomeAddress);

        var hobbiesField = schema.Fields.FirstOrDefault(f => f.Name == "Hobbies");
        person.Put(hobbiesField!.Pos, hobbies);
        Assert.Equal(hobbies, person.Hobbies);

        var metadataField = schema.Fields.FirstOrDefault(f => f.Name == "Metadata");
        person.Put(metadataField!.Pos, metadata);
        Assert.Equal(metadata, person.Metadata);

        var genderField = schema.Fields.FirstOrDefault(f => f.Name == "Gender");
        person.Put(genderField!.Pos, GenderEnum.Female);
        Assert.Equal(GenderEnum.Female, person.Gender);
    }

    [Fact]
    public void Generated_Get_Method_Should_Have_All_Properties_In_Order()
    {
        // This test verifies the source generator creates Get with all properties
        var person = new Person();
        var schema = person.Schema as RecordSchema;
        Assert.NotNull(schema);

        // The generator should create a Get method that returns each property by position
        // We test this by ensuring no ArgumentOutOfRangeException for valid positions
        for (int i = 0; i < schema.Count; i++)
        {
            var exception = Record.Exception(() => person.Get(i));
            Assert.Null(exception); // Should not throw for valid positions
        }
    }
}