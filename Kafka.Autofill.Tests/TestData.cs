using System.Globalization;

namespace Kafka.Autofill.Tests;

public static class TestData
{
    public static Person Person => new()
    {
        EventId = Guid.Parse("57BD895C-D530-47D3-B501-4345170D59B9"),
        Age = 30,
        UnsignedAge = 30u,
        LongValue = 1000000L,
        UnsignedLongValue = 1000000UL,
        Height = 5.9f,
        Weight = 180.5,
        Salary = 75000.50m,
        IsActive = true,
        Name = "John Doe",
        ProfilePicture = [1, 2, 3],
        BirthDate = new DateTime(1994, 1, 15),
        DateOfBirth = DateOnly.FromDateTime(DateTime.Parse("10/02/2026 12:06:44", CultureInfo.CurrentCulture)),
        OptionalDateOfBirthNonNull = DateOnly.FromDateTime(DateTime.Parse("10/02/2026 12:06:44", CultureInfo.CurrentCulture)),
        OptionalDateOfBirthNull = null,
        CreatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
        Id = Guid.Parse("B381A45F-0656-4452-80ED-D734C05C6329"),
        OptionalAge = 25,
        OptionalUint = 10000u,
        OptionalWeight = 175.5,
        OptionalSalary = 75000.50m,
        OptionalFlag = true,
        OptionalDate = new DateTime(2023, 1, 1),
        OptionalId = Guid.Parse("B8E24163-84D2-442E-8ECF-441F5A4FE2D8"),
        OptionalUnsignedLongValue = 1000000UL,
        Scores = [85, 90, 95],
        Tags = ["developer", "tester"],
        Hobbies = ["reading", "coding"],
        Skills = ["C#", "Python"],
        Ratings = new List<int> { 1, 2, 3 },
        Achievements = new List<string> { "Award1" },
        Measurements = new List<double> { 1.1, 2.2 },
        Metadata = new Dictionary<string, string> { { "key1", "value1" } },
        Stats = new Dictionary<string, int> { { "points", 100 } },
        Gender = GenderEnum.Male,
        OptionalGender = GenderEnum.Female,
        HomeAddress = new Address { Street = "123 Main St", City = "NYC", ZipCode = "10001" },
        OfficeAddress = new Address { Street = "456 Work Ave", City = "LA", ZipCode = "90001" },
        PreviousAddresses = [new Address { Street = "789 Old Rd", City = "Chicago", ZipCode = "60601" }],
        AddressBook = new Dictionary<string, Address>
        {
            { "home", new Address { Street = "Home St", City = "Boston", ZipCode = "02101" } }
        }
    };
}