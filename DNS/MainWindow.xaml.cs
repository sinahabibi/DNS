using DNS.Data;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using DNS.Model;

namespace DNS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ApplicationDbContext _context = new ApplicationDbContext();
        private bool isDnsActive = false;
        private readonly VisibilityViewModel _visibilityViewModel;
        private bool isChangingState = false; // متغیر کمکی

        public MainWindow()
        {
            InitializeComponent();


            _visibilityViewModel = new VisibilityViewModel();
            this.DataContext = _visibilityViewModel;

            _ = LoaderAsync();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private static async Task<string> GetInternetConnectedNetworkInterfaceAsync()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var ipProperties = ni.GetIPProperties();
                    if (ipProperties.GatewayAddresses.Any(g => g.Address.AddressFamily.ToString() == "InterNetwork"))
                    {
                        return ni.Name;
                    }
                }
            }
            return null;
        }

        private async Task SetDNSAsync(string interfaceName, int dnsId)
        {
            string primaryDNS = "";
            string secondaryDNS = "";
            if (dnsId != 0)
            {
                var dns = await _context.Dns.FindAsync(dnsId);
                primaryDNS = dns.primeryDns;
                secondaryDNS = dns.secondaryDns;
            }

            try
            {
                // Set primary DNS
                string primaryCmd = $"netsh interface ip set dns name=\"{interfaceName}\" source=static addr={primaryDNS}";
                await ExecuteCmdCommandAsync(primaryCmd);

                // Add secondary DNS
                string secondaryCmd = $"netsh interface ip add dns name=\"{interfaceName}\" addr={secondaryDNS} index=2";
                await ExecuteCmdCommandAsync(secondaryCmd);

                //MessageBox.Show($"DNS settings updated successfully. \n Interface Name: {interfaceName} \n PrimaryDns: {primaryDNS}\n SecondaryDns: {secondaryDNS}\nDNSId: {dnsId}");
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception: " + e.Message);
                throw;
            }
        }

        private static async Task ExecuteCmdCommandAsync(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using (Process process = Process.Start(psi))
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    MessageBox.Show("Error executing command: " + error);
                }
            }
        }

        private static async Task<List<string>> GetCurrentDNSAsync(string interfaceName)
        {
            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in interfaces)
                {
                    if (ni.Name == interfaceName)
                    {
                        var ipProperties = ni.GetIPProperties();
                        var dnsAddresses = ipProperties.DnsAddresses;
                        List<string> dnsList = new List<string>();
                        foreach (var dns in dnsAddresses)
                        {
                            dnsList.Add(dns.ToString());
                        }
                        return dnsList;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception: " + e.Message);
                throw;
            }
            return null;
        }

        private async Task LoadInterfacesAsync()
        {
            cb_interface.Items.Clear();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                cb_interface.Items.Add(ni.Name);
            }
            string interfaceName = await GetInternetConnectedNetworkInterfaceAsync();
            cb_interface.SelectedValue = interfaceName;
        }

        private async Task LoadDnsAsync()
        {
            await _context.Database.EnsureCreatedAsync();
            await _context.Dns.LoadAsync();
            var dnsList = _context.Dns.Local.ToObservableCollection();
            cb_Dns.ItemsSource = dnsList;
            cb_Dns.DisplayMemberPath = "Name";
            cb_Dns.SelectedValuePath = "Id";

            var selectedInterface = cb_interface.SelectedValue;
            if (selectedInterface != null)
            {
                var currentDnsList = await GetCurrentDNSAsync(selectedInterface.ToString());
                var firstDns = dnsList.FirstOrDefault();
                if (firstDns != null)
                {
                    cb_Dns.SelectedValue = firstDns.Id;
                }
                if (currentDnsList != null)
                {
                    foreach (var dnsItem in dnsList)
                    {
                        bool isPrimaryMatched = currentDnsList.Any(currentDns => currentDns == dnsItem.primeryDns);
                        bool isSecondaryMatched = currentDnsList.Any(currentDns => currentDns == dnsItem.secondaryDns);

                        if (isPrimaryMatched && isSecondaryMatched)
                        {
                            cb_Dns.SelectedValue = dnsItem.Id;
                            btn_turnOnOrOff.Background = new SolidColorBrush(Color.FromRgb(121, 183, 0));
                            isDnsActive = true;
                            lbl_dnsStatus.Content = "Dns is active";
                            break;
                        }
                    }
                }
            }
        }

        private async Task LoaderAsync()
        {
            await LoadInterfacesAsync();
            await LoadDnsAsync();
            await CheckDnsSetAsync();
        }

        private async void btn_addDns_Click(object sender, RoutedEventArgs e)
        {
            AddOrEditDns addOrEditDns = new AddOrEditDns(0);
            addOrEditDns.ShowDialog();
            await LoaderAsync();
        }

        private async void btn_turnOnOrOff_Click(object sender, RoutedEventArgs e)
        {
            var selectedInterface = cb_interface.SelectedValue;
            var selectedDns = cb_Dns.SelectedValue;

            var dnsList = _context.Dns;
            if (selectedInterface != null && selectedDns != null)
            {
                await CheckDnsSetAsync();
                lbl_dnsStatus.Content = "Please wait...";
                if (!isDnsActive)
                {
                    await SetDNSAsync(selectedInterface.ToString(), (int)selectedDns);
                }
                else
                {
                    await SetDNSAsync(selectedInterface.ToString(), 0);
                }

                await CheckDnsSetAsync();
            }
            else
            {
                MessageBox.Show("Please select an item!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<int> CheckDnsSetAsync()
        {
            try
            {
                var selectedInterface = cb_interface.SelectedValue;
                var selectedDns = cb_Dns.SelectedValue;
                if (selectedDns == null || selectedInterface == null)
                {
                    // Handle the case where selectedDns is null
                    return 0;
                }
                var currentDnsList = await GetCurrentDNSAsync(selectedInterface.ToString());
                var dnsList = _context.Dns.Where(dns => (int)selectedDns == dns.Id);

                if (currentDnsList != null)
                {
                    foreach (var dnsItem in dnsList)
                    {
                        bool isPrimaryMatched = currentDnsList.Any(currentDns => currentDns == dnsItem.primeryDns);
                        bool isSecondaryMatched = currentDnsList.Any(currentDns => currentDns == dnsItem.secondaryDns);

                        if (isPrimaryMatched && isSecondaryMatched)
                        {
                            cb_Dns.SelectedValue = dnsItem.Id;
                            btn_turnOnOrOff.Background = new SolidColorBrush(Color.FromRgb(121, 183, 0));
                            isDnsActive = true;
                            lbl_dnsStatus.Content = "Dns is active";
                            return dnsItem.Id;
                            break;
                        }
                        else
                        {
                            btn_turnOnOrOff.Background = new SolidColorBrush(Color.FromRgb(148, 0, 211));
                            lbl_dnsStatus.Content = "Dns is not active!";
                            isDnsActive = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return 0;
        }

        private async void cb_Dns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await CheckDnsSetAsync();
        }

        private async void cb_interface_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await CheckDnsSetAsync();
        }

        private async void btn_deleteDns_Click(object sender, RoutedEventArgs e)
        {
            var selectedDns = cb_Dns.SelectedValue;
            var dns = _context.Dns.Find(selectedDns);
            if (dns != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {dns.Name} dns?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _context.Dns.Remove(dns);
                    await _context.SaveChangesAsync();
                    MessageBox.Show("Dns deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoaderAsync();
                }

            }
        }

        private async void btn_editdns_Click(object sender, RoutedEventArgs e)
        {
            var selectedDns = cb_Dns.SelectedValue;
            var dns = await _context.Dns.FindAsync(selectedDns);
            if (dns != null)
            {
                AddOrEditDns addOrEditDns = new AddOrEditDns(dns.Id);
                addOrEditDns.ShowDialog();

                var selectedInterface = cb_interface.SelectedValue;
                int dnsId = await CheckDnsSetAsync();
                if (dns.Id == dnsId)
                {
                    lbl_dnsStatus.Content = "Please wait...";

                    await SetDNSAsync(selectedInterface.ToString(), (int)selectedDns);
                    await CheckDnsSetAsync();
                }
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

        private async void btn_close_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation();
            await FadeAnimation(1, 0);


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

        private async void btn_minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private async void Window_StateChanged(object? sender, EventArgs e)
        {
            if (isChangingState) return; // جلوگیری از اجرای کد در هنگام تغییر وضعیت

            if (this.WindowState == WindowState.Normal)
            {
                br_main.Opacity = 0;
                await FadeAnimation(0, 1);

            }

            if (this.WindowState == WindowState.Minimized)
            {
                isChangingState = true; // تنظیم متغیر کمکی
                WindowState = WindowState.Normal;

                await FadeAnimation(1, 0);

                this.WindowState = WindowState.Minimized;
                br_main.Opacity = 1;
                isChangingState = false; // بازنشانی متغیر کمکی
            }
        }
        #endregion

        private async Task FadeAnimation(double from,double to,double duration=.2)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };

            br_main.BeginAnimation(Border.OpacityProperty, animation);

            await Task.Delay(animation.Duration.TimeSpan);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await FadeAnimation(0, 1);
        }
    }
}
