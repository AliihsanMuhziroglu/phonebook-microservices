using System;

namespace ContactService.Models
{
    public enum ContactInfoType
    {
        Phone = 0,
        Email = 1,
        Location = 2
    }

    public class ContactInfo
    {
        public Guid Id { get; set; }
        public ContactInfoType Type { get; set; }
        public string Value { get; set; } = null!;
        public Guid ContactId { get; set; }
        public Contact? Contact { get; set; }
    }
}
