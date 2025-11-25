using System;

namespace XqueezeOS.Models
{
    /// <summary>
    /// Contact model for storing contact information
    /// </summary>
    public class Contact
    {
        /// <summary>
        /// Unique identifier for the contact
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Full name of the contact
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// When the contact was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Calculates approximate size in bytes (character count)
        /// </summary>
        public long GetSizeInBytes()
        {
            long size = 0;
            size += FullName?.Length ?? 0;
            size += Phone?.Length ?? 0;
            size += Email?.Length ?? 0;
            // Approximate size for Guid (16 bytes) and DateTime (8 bytes)
            size += 24;
            return size;
        }

        /// <summary>
        /// String representation of contact
        /// </summary>
        public override string ToString()
        {
            return $"{FullName} - {Phone}";
        }
    }
}
