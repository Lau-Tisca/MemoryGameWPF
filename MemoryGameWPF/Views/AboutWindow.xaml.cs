using System.Diagnostics; // For Process.Start
using System.Net.Mail; // For MailTo Uri scheme validation (optional but good)
using System.Windows;
using System.Windows.Navigation; // For RequestNavigateEventArgs

namespace MemoryGameWPF.Views
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Validate if it's a mailto link (optional but safer)
            if (e.Uri != null && e.Uri.Scheme == Uri.UriSchemeMailto)
            {
                try
                {
                    // Use Process.Start for mailto links
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                }
                catch (System.ComponentModel.Win32Exception noBrowser)
                {
                    // Handle case where no default mail client is configured
                    if (noBrowser.ErrorCode == -2147467259)
                        MessageBox.Show("Could not find a default mail client to open the link.", "Mail Client Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    // Handle other potential errors
                    MessageBox.Show($"Could not open mail link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            // Optional: Handle other URI schemes if needed
            // else if (e.Uri != null && (e.Uri.Scheme == Uri.UriSchemeHttp || e.Uri.Scheme == Uri.UriSchemeHttps)) { ... }

            e.Handled = true; // Indicate that we've handled the navigation
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Close the About window
        }
    }
}