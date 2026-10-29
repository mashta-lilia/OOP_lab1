namespace LabCalculator.App;

public partial class HelpPage : ContentPage
{
	public HelpPage()
	{
		InitializeComponent();
	}

	private async void OnCloseClicked(object? sender, EventArgs args)
	{
		try
		{
			await Navigation.PopAsync();
		}
		catch (InvalidOperationException)
		{
		}
	}
}
