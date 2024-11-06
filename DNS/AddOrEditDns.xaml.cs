using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DNS.Data;
using DNS.Models;

namespace DNS
{
    /// <summary>
    /// Interaction logic for AddOrEditDns.xaml
    /// </summary>
    public partial class AddOrEditDns : Window
    {
        private readonly AddOrEditVisibilityViewModel _visibilityViewModel;

        private readonly ApplicationDbContext _context =
            new ApplicationDbContext();

        public int Id { get; set; }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void TextBoxPastingPrimary(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowedForPaste(text))
                {
                    e.CancelCommand();
                }

                // Check if the format is x.x.x.x
                var parts = text.Split('.');
                if (parts.Length == 4 && parts.All(part => int.TryParse(part, out int num) && num >= 0 && num <= 255))
                {
                    // Assign each part to the corresponding TextBox
                    txt_pd_1.Text = parts[0];
                    txt_pd_2.Text = parts[1];
                    txt_pd_3.Text = parts[2];
                    txt_pd_4.Text = parts[3];
                    txt_pd_4.CaretIndex = txt_pd_4.Text.Length;
                    txt_pd_4.Focus();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void TextBoxPastingSecondary(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowedForPaste(text))
                {
                    e.CancelCommand();
                }

                // Check if the format is x.x.x.x
                var parts = text.Split('.');
                if (parts.Length == 4 && parts.All(part => int.TryParse(part, out int num) && num >= 0 && num <= 255))
                {
                    // Assign each part to the corresponding TextBox
                    txt_sd_1.Text = parts[0];
                    txt_sd_2.Text = parts[1];
                    txt_sd_3.Text = parts[2];
                    txt_sd_4.Text = parts[3];
                    txt_sd_4.CaretIndex = txt_sd_4.Text.Length;
                    txt_sd_4.Focus();

                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]+"); // Only allow numbers
            return !regex.IsMatch(text) && int.TryParse(text, out int result) && result <= 255;
        }

        private static bool IsTextAllowedForPaste(string text)
        {
            Regex regex = new Regex("[^0-9.]+"); // Allow numbers and dot
            return !regex.IsMatch(text) && int.TryParse(text, out int result) && result <= 255;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (e.Key == Key.Back && textBox.Text.Length == 0)
            {
                // Move to the previous TextBox if Backspace is pressed and the current TextBox is empty
                MoveToPreviousTextBox(textBox);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (int.TryParse(textBox.Text, out int value))
            {
                if (value > 255)
                {
                    textBox.Text = "255";
                    textBox.CaretIndex = textBox.Text.Length; // Move caret to the end
                }
            }

            if (textBox.Text.Length == 3)
            {
                // Move to the next TextBox if the current TextBox has 3 characters
                MoveToNextTextBox(textBox);
            }
        }

        private void MoveToNextTextBox(TextBox currentTextBox)
        {
            if (currentTextBox == txt_pd_1) txt_pd_2.Focus();
            else if (currentTextBox == txt_pd_2) txt_pd_3.Focus();
            else if (currentTextBox == txt_pd_3) txt_pd_4.Focus();
            else if (currentTextBox == txt_pd_4) txt_sd_1.Focus();
            else if (currentTextBox == txt_sd_1) txt_sd_2.Focus();
            else if (currentTextBox == txt_sd_2) txt_sd_3.Focus();
            else if (currentTextBox == txt_sd_3) txt_sd_4.Focus();
        }

        private void MoveToPreviousTextBox(TextBox currentTextBox)
        {
            if (currentTextBox == txt_pd_2)
            {
                txt_pd_1.Focus();
                txt_pd_1.SelectAll();
            }
            else if (currentTextBox == txt_pd_3)
            {
                txt_pd_2.Focus();
                txt_pd_2.SelectAll();
            }
            else if (currentTextBox == txt_pd_4)
            {
                txt_pd_3.Focus();
                txt_pd_3.SelectAll();
            }
            else if (currentTextBox == txt_sd_1)
            {
                txt_pd_4.Focus();
                txt_pd_4.SelectAll();
            }
            else if (currentTextBox == txt_sd_2)
            {
                txt_sd_1.Focus();
                txt_sd_1.SelectAll();
            }
            else if (currentTextBox == txt_sd_3)
            {
                txt_sd_2.Focus();
                txt_sd_2.SelectAll();
            }
            else if (currentTextBox == txt_sd_4)
            {
                txt_sd_3.Focus();
                txt_sd_3.SelectAll();
            }
        }

        public AddOrEditDns(int id)
        {
            InitializeComponent();
            Id = id;
            _visibilityViewModel = new AddOrEditVisibilityViewModel();
            this.DataContext = _visibilityViewModel;
            if (Id != 0)
            {
                var dns = _context.Dns.Find(Id);
                txt_name.Text = dns.Name;
                var primaryDns = dns.primeryDns.Split('.');
                txt_pd_1.Text = primaryDns[0];
                txt_pd_2.Text = primaryDns[1];
                txt_pd_3.Text = primaryDns[2];
                txt_pd_4.Text = primaryDns[3];
                var secondaryDns = dns.secondaryDns.Split('.');
                txt_sd_1.Text = secondaryDns[0];
                txt_sd_2.Text = secondaryDns[1];
                txt_sd_3.Text = secondaryDns[2];
                txt_sd_4.Text = secondaryDns[3];
                btn_save.Content = "Edit";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        #region Contorller Button

        private void btn_minimize_MouseEnter(object sender, MouseEventArgs e)
        {
            _visibilityViewModel.BtnMinimizeVisibility = Visibility.Visible;
        }

        private void btn_minimize_MouseLeave(object sender, MouseEventArgs e)
        {
            _visibilityViewModel.BtnMinimizeVisibility = Visibility.Hidden;
        }

        private void btn_maximize_MouseEnter(object sender, MouseEventArgs e)
        {
            _visibilityViewModel.BtnMaximizeVisibility = Visibility.Visible;
        }

        private void btn_maximize_MouseLeave(object sender, MouseEventArgs e)
        {
            _visibilityViewModel.BtnMaximizeVisibility = Visibility.Hidden;
        }

        private void btn_close_MouseEnter(object sender, MouseEventArgs e)
        {
            _visibilityViewModel.BtnCloseVisibility = Visibility.Visible;
        }

        private void btn_close_MouseLeave(object sender, MouseEventArgs e)
        {
            _visibilityViewModel.BtnCloseVisibility = Visibility.Hidden;
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void btn_minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        #endregion

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txt_name.Text))
            {
                if (!string.IsNullOrEmpty(txt_pd_1.Text) || !string.IsNullOrEmpty(txt_pd_2.Text) || !string.IsNullOrEmpty(txt_pd_3.Text) || !string.IsNullOrEmpty(txt_pd_4.Text))
                {

                    if (Id == 0)
                    {
                        string primaryDns = $"{txt_pd_1.Text}.{txt_pd_2.Text}.{txt_pd_3.Text}.{txt_pd_4.Text}";
                        string secondaryDns = $"{txt_sd_1.Text}.{txt_sd_2.Text}.{txt_sd_3.Text}.{txt_sd_4.Text}";
                        Dns dns = new Dns
                        {
                            Name = txt_name.Text,
                            primeryDns = primaryDns,
                            secondaryDns = secondaryDns
                        };

                        _context.Dns.Add(dns);
                        _context.SaveChanges();
                    }
                    else
                    {
                        var existingDns = _context.Dns.Find(Id);
                        if (existingDns != null)
                        {
                            existingDns.Name = txt_name.Text;
                            existingDns.primeryDns = $"{txt_pd_1.Text}.{txt_pd_2.Text}.{txt_pd_3.Text}.{txt_pd_4.Text}";
                            existingDns.secondaryDns = $"{txt_sd_1.Text}.{txt_sd_2.Text}.{txt_sd_3.Text}.{txt_sd_4.Text}";

                            _context.Dns.Update(existingDns);
                            _context.SaveChanges();
                        }

                    }
                    this.Close();

                }
                else
                {
                    MessageBox.Show("Please fill primary dns", "DNS", MessageBoxButton.OK);
                }
            }
            else
            {
                MessageBox.Show("Please fill name", "DNS", MessageBoxButton.OK);
            }
        }
    }
}
