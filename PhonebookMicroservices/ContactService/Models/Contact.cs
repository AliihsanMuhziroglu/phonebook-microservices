using System;
using System.Collections.Generic;

namespace ContactService.Models
{
    public class Contact
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Company { get; set; }
        public List<ContactInfo> ContactInfos { get; set; } = new();
    }
}
