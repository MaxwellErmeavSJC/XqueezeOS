using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using XqueezeOS.Models;

namespace XqueezeOS.Storage
{
    public static class ContactStorage
    {
        private static string FilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "contacts.json");

        public static List<Contact> LoadContacts()
        {
            if (!File.Exists(FilePath))
                return new List<Contact>();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<Contact>>(json);
        }

        public static void SaveContacts(List<Contact> contacts)
        {
            string json = JsonSerializer.Serialize(contacts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        public static long GetTotalDataSize(List<Contact> contacts)
        {
            long total = 0;
            foreach (var contact in contacts)
                total += contact.GetSizeInBytes();
            return total;
        }
    }
}
