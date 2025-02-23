namespace NetworkMonitor;

public partial class LogsPage : ContentPage
{
    public LogsPage(string logs)
    {
        InitializeComponent(); 
        logsText.Text = logs;  
    }

    // Obs?uga przycisku zamykaj?cego stron? log�w
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync(); // Powr�t do poprzedniej strony w nawigacji
    }
}