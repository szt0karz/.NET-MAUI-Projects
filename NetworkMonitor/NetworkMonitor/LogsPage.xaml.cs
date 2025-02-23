namespace NetworkMonitor;

public partial class LogsPage : ContentPage
{
    public LogsPage(string logs)
    {
        InitializeComponent(); 
        logsText.Text = logs;  
    }

    // Obs?uga przycisku zamykaj?cego stron? logów
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync(); // Powrót do poprzedniej strony w nawigacji
    }
}