using LabCalculator.Core;
using LabCalculator.Core.Storage;
using Microsoft.Extensions.Logging;
#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
#endif

namespace LabCalculator.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		MauiAppBuilder builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<ITableStorage, JsonTableStorage>();
		builder.Services.AddSingleton<TableController>();
		builder.Services.AddSingleton<MainPage>();

#if WINDOWS
		// Закриття вікна кнопкою «X» (або Alt+F4) теж має запропонувати зберегти зміни.
		builder.ConfigureLifecycleEvents(events =>
			events.AddWindows(windows =>
				windows.OnWindowCreated(window => window.AppWindow.Closing += OnWindowClosing)));
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

#if WINDOWS
	private static async void OnWindowClosing(
		Microsoft.UI.Windowing.AppWindow sender,
		Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
	{
		var mainPage = IPlatformApplication.Current?.Services.GetService(typeof(MainPage)) as MainPage;
		if (mainPage is null || !mainPage.NeedsCloseConfirmation)
		{
			return;
		}

		// Cancel треба встановити до першого await: одразу після нього Windows прочитає це значення.
		args.Cancel = true;
		bool canClose = await mainPage.ConfirmCloseAsync();
		if (canClose)
		{
			Application.Current?.Quit();
		}
	}
#endif
}
