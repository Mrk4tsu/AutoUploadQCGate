using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutoUploadQCGate
{
    /// <summary>
    /// Interaction logic for PasswordInputWindow.xaml
    /// </summary>
    public partial class PasswordInputWindow : Window
    {
        private readonly string _appPassword;

        public bool IsAuthenticated { get; private set; } = false;

        public PasswordInputWindow(string appPassword)
        {
            InitializeComponent();
            _appPassword = appPassword;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBoxInput.Password == _appPassword)
            {
                IsAuthenticated = true;
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
                PasswordBoxInput.Clear();
                PasswordBoxInput.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
