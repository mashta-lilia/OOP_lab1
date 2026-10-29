namespace LabCalculator.App;

public partial class App : Application
{
	private readonly MainPage _mainPage;

	public App(MainPage mainPage)
	{
		InitializeComponent();
		_mainPage = mainPage;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Без NavigationPage на Windows не показується головне меню.
		return new Window(new NavigationPage(_mainPage)) { Title = "Електронна таблиця" };
	}
}
