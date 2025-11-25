using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using XqueezeOS.Models;
using XqueezeOS.Storage;

namespace XqueezeOS.Views
{
    public partial class ContactsPage : UserControl
    {
        private List<Contact> contacts;

        public ContactsPage()
        {
            InitializeComponent();
            contacts = ContactStorage.LoadContacts();
            RefreshUI();
        }

        private void SaveContact_Click(object sender, RoutedEventArgs e)
        {
            Contact newContact = new Contact
            {
                FullName = NameBox.Text,
                Phone = PhoneBox.Text,
                Email = EmailBox.Text
            };

            contacts.Add(newContact);
            ContactStorage.SaveContacts(contacts);
            RefreshUI();

            NameBox.Clear();
            PhoneBox.Clear();
            EmailBox.Clear();
        }

        private void RefreshUI()
        {
            ContactsList.ItemsSource = null;
            ContactsList.ItemsSource = contacts;

            SizeText.Text = $"Total Storage Size: {ContactStorage.GetTotalDataSize(contacts)} bytes";
        }
    }
}
