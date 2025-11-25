using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using XqueezeOS.Models;
using XqueezeOS.Storage;

namespace XqueezeOS.Views
{
    /// <summary>
    /// Contacts Page - Manage contact information
    /// </summary>
    public partial class ContactsPage : UserControl
    {
        private List<Contact> contacts;

        public ContactsPage()
        {
            InitializeComponent();
            LoadContacts();
        }

        /// <summary>
        /// Load contacts from storage
        /// </summary>
        private void LoadContacts()
        {
            try
            {
                contacts = ContactStorage.LoadContacts();
                RefreshUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading contacts: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                contacts = new List<Contact>();
            }
        }

        /// <summary>
        /// Save contact button click handler
        /// </summary>
        private void SaveContact_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    MessageBox.Show("Please enter a name.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NameBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhoneBox.Text))
                {
                    MessageBox.Show("Please enter a phone number.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PhoneBox.Focus();
                    return;
                }

                // Create new contact
                Contact newContact = new Contact
                {
                    FullName = NameBox.Text.Trim(),
                    Phone = PhoneBox.Text.Trim(),
                    Email = EmailBox.Text.Trim()
                };

                // Add to list
                contacts.Add(newContact);

                // Save to storage
                ContactStorage.SaveContacts(contacts);

                // Refresh UI
                RefreshUI();

                // Clear inputs
                ClearInputs();

                // Show success message
                MessageBox.Show($"Contact '{newContact.FullName}' saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving contact: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Refresh the UI with current contacts
        /// </summary>
        private void RefreshUI()
        {
            try
            {
                // Refresh contacts list
                ContactsList.ItemsSource = null;
                ContactsList.ItemsSource = contacts;

                // Update size display
                long sizeInBytes = ContactStorage.GetTotalDataSize(contacts);
                double sizeInKB = sizeInBytes / 1024.0;

                SizeText.Text = $"Total Contacts: {contacts.Count} | " +
                              $"Storage Size: {sizeInKB:F2} KB";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all input fields
        /// </summary>
        private void ClearInputs()
        {
            NameBox.Clear();
            PhoneBox.Clear();
            EmailBox.Clear();
            NameBox.Focus();
        }
    }
}
