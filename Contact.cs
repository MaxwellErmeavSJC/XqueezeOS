using System;

namespace XqueezeOS.Models
{
    public class Contact
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public long GetSizeInBytes()
        {
            long size = 0;
            size += FullName?.Length ?? 0;
            size += Phone?.Length ?? 0;
            size += Email?.Length ?? 0;
            return size;
        }
    }
}
