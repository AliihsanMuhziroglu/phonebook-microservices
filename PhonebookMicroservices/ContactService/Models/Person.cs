using System.Text.Json.Serialization;

namespace ContactService.Models;

public enum ContactType { Phone, Email, Location }

public class Person
{
    public Guid UUID { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Company { get; set; }

    public List<ContactInfo> ContactInfos { get; set; } = new();
}

public class ContactInfo
{
    public Guid UUID { get; set; } = Guid.NewGuid();
    public ContactType Type { get; set; }
    public string Value { get; set; } = default!;

    public Guid PersonUUID { get; set; }
    [JsonIgnore]
    public Person Person { get; set; } = default!;
}
