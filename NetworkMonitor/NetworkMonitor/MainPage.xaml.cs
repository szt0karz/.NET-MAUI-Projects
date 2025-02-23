using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkMonitor
{
    public partial class MainPage : ContentPage
    {
        private UdpClient _udpReceiver; // Obiekt do odbioru pakietów UDP
        private bool _isMonitoring; // Flaga określająca, czy monitoring jest aktywny
        private readonly string _logPath = Path.Combine(FileSystem.AppDataDirectory, "network_logs.txt"); // Ścieżka do pliku logów
        private int _packetCount; // Licznik odebranych pakietów

        public MainPage()
        {
            InitializeComponent();
            LoadSavedConfig(); // Wczytanie zapisanych ustawień
        }

        private void LoadSavedConfig()
        {
            // Wczytanie ostatnio używanego adresu IP i portu
            if (Preferences.ContainsKey("LastIP"))
                ipEntry.Text = Preferences.Get("LastIP", "239.255.255.250");

            if (Preferences.ContainsKey("LastPort"))
                portEntry.Text = Preferences.Get("LastPort", "5060");
        }

        private async void OnToggleMonitoring(object sender, EventArgs e)
        {
            if (_isMonitoring)
            {
                StopMonitoring(); // Zatrzymanie monitorowania
                return;
            }

            // Sprawdzenie poprawności adresu IP
            if (!IPAddress.TryParse(ipEntry.Text, out var ipAddress))
            {
                await DisplayAlert("Błąd", "Nieprawidłowy adres IP", "OK");
                return;
            }

            // Sprawdzenie poprawności portu
            if (!int.TryParse(portEntry.Text, out var port) || port < 1 || port > 65535)
            {
                await DisplayAlert("Błąd", "Nieprawidłowy port", "OK");
                return;
            }

            StartMonitoring(ipAddress, port); // Rozpoczęcie monitorowania
        }

        private void StartMonitoring(IPAddress ip, int port)
        {
            try
            {
                _udpReceiver = new UdpClient(); // Utworzenie klienta UDP
                _udpReceiver.Client.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true
                );
                _udpReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, port)); // Powiązanie z wybranym portem
                _udpReceiver.JoinMulticastGroup(ip); // Dołączenie do grupy multicastowej

                _isMonitoring = true;
                monitoringBtn.Text = "Stop Monitoring";
                statusLabel.Text = $"Monitoring: {ip}:{port}";

                // Zapisanie ostatniego używanego IP i portu
                Preferences.Set("LastIP", ip.ToString());
                Preferences.Set("LastPort", port.ToString());

                // Asynchroniczne nasłuchiwanie pakietów
                Task.Run(async () =>
                {
                    while (_isMonitoring)
                    {
                        var result = await _udpReceiver.ReceiveAsync(); // Odbiór pakietu
                        var message = Encoding.ASCII.GetString(result.Buffer);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            // Dodanie wiadomości do logu na ekranie
                            logText.Text += $"[{DateTime.Now:T}] {message}\n";
                            if (logText.Text.Length > 10000)
                                logText.Text = logText.Text[^5000..];
                            UpdateTrafficStats(result.Buffer.Length);
                        });

                        await LogToFile($"[{DateTime.Now:u}] {message}"); // Zapis do pliku logów
                    }
                });
            }
            catch (Exception ex)
            {
                DisplayAlert("Błąd", ex.Message, "OK"); // Obsługa błędów
            }
        }

        private void StopMonitoring()
        {
            _isMonitoring = false;
            _udpReceiver?.Close(); // Zamknięcie klienta UDP
            monitoringBtn.Text = "Start Monitoring";
            statusLabel.Text = "Monitoring zatrzymany";
        }

        private void UpdateTrafficStats(int packetSize)
        {
            // Aktualizacja sumy odebranych bajtów
            var totalBytes = double.Parse(trafficLabel.Text.Split(' ')[0]) + packetSize;
            trafficLabel.Text = $"{totalBytes} B";

            // Aktualizacja liczby pakietów
            _packetCount++;
            packetCountLabel.Text = $"{_packetCount} pakietów";
        }

        private async Task LogToFile(string message)
        {
            try
            {
                await File.AppendAllTextAsync(_logPath, message + "\n"); // Dopisanie logów do pliku
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd logowania", ex.Message, "OK");
            }
        }

        private async void OnShowLogs(object sender, EventArgs e)
        {
            // Sprawdzenie, czy plik logów istnieje
            if (!File.Exists(_logPath))
            {
                await DisplayAlert("Info", "Brak logów", "OK");
                return;
            }

            var logs = await File.ReadAllTextAsync(_logPath); // Odczyt logów
            await Navigation.PushAsync(new LogsPage(logs)); // Przejście do strony logów
        }

        private async void OnSendTestPacket(object sender, EventArgs e)
        {
            // Sprawdzenie poprawności adresu IP
            if (!IPAddress.TryParse(ipEntry.Text, out var ipAddress))
            {
                await DisplayAlert("Błąd", "Nieprawidłowy adres IP", "OK");
                return;
            }

            // Sprawdzenie poprawności portu
            if (!int.TryParse(portEntry.Text, out var port) || port < 1 || port > 65535)
            {
                await DisplayAlert("Błąd", "Nieprawidłowy port", "OK");
                return;
            }

            try
            {
                using var client = new UdpClient(); // Utworzenie klienta UDP
                var message = $"TEST {DateTime.Now:T}";
                var data = Encoding.ASCII.GetBytes(message);
                await client.SendAsync(data, data.Length, new IPEndPoint(ipAddress, port)); // Wysłanie pakietu
                await DisplayAlert("Sukces", "Pakiet testowy wysłany", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", ex.Message, "OK");
            }
        }
    }
}
