// Головне вікно, частина 1 з 4: поля, конструктор і обробники меню та кнопок.

using LabCalculator.Core;

namespace LabCalculator.App;

/// <summary>Головне вікно: лише передає команди TableController і показує таблицю.</summary>
public partial class MainPage : ContentPage
{
	private const string AppTitle = "Електронна таблиця";
	private const string NewTableName = "Нова таблиця";
	private const string ErrorMarker = "#ПОМИЛКА";
	private const string NoSelectionText = "—";

	private const string ModeValueText = "Режим: ЗНАЧЕННЯ";
	private const string ModeExpressionText = "Режим: ВИРАЗ";
	private const string SwitchToExpressionHint = "Перемкнути на ВИРАЗ";
	private const string SwitchToValueHint = "Перемкнути на ЗНАЧЕННЯ";

	private const string CycleTitle = "Циклічне посилання";
	private const string CycleMessage = "Циклічне посилання — вираз не прийнято";
	private const string ErrorTitle = "Помилка";

	private const string AnswerYes = "Так";
	private const string AnswerNo = "Ні";
	private const string AnswerCancel = "Скасувати";
	private const string AnswerOk = "OK";

	private const string DeleteWarning =
		"Дані в ньому буде втрачено, а посилання на нього стануть помилкою #ПОСИЛАННЯ!.";

	private const double HeaderColumnWidth = 48;
	private const double CellWidth = 110;
	private const double RowHeight = 36;
	private const double CellFontSize = 14;

	private const string AboutText =
		"Електронна таблиця\n\n" +
		"Лабораторна робота №1, варіант 8\n" +
		"Автор: Лілія Машта, група К-27\n" +
		"Під час розробки використовувався генеративний ШІ (Claude) — відповідно до рівня 2.\n" +
		"2026\n\n" +
		"C# / .NET MAUI / ANTLR4";

	private static readonly Color _cellTextLight = Colors.Black;
	private static readonly Color _cellTextDark = Colors.White;
	private static readonly Color _errorTextLight = Color.FromArgb("#C62828");
	private static readonly Color _errorTextDark = Color.FromArgb("#FF8A80");
	private static readonly Color _cellBackgroundLight = Colors.White;
	private static readonly Color _cellBackgroundDark = Color.FromArgb("#1F1F1F");
	private static readonly Color _selectedBackgroundLight = Color.FromArgb("#DFD8F7");
	private static readonly Color _selectedBackgroundDark = Color.FromArgb("#3B2C78");
	private static readonly Color _headerBackgroundLight = Color.FromArgb("#E1E1E1");
	private static readonly Color _headerBackgroundDark = Color.FromArgb("#404040");

	private readonly TableController _controller;

	private readonly Dictionary<string, Entry> _entriesByAddress = new();
	private readonly Dictionary<Entry, string> _addressesByEntry = new();

	// Що вже показано в кожному полі, щоб не оновлювати однакові властивості повторно.
	private readonly Dictionary<Entry, RenderedState> _renderedStates = new();

	private readonly List<Label> _rowHeaders = new();
	private readonly List<Label> _columnHeaders = new();

	private DisplayMode _displayMode = DisplayMode.Value;
	private string? _selectedAddress = null;

	// Клітинка в режимі редагування: її текст ще може бути не записаний у таблицю.
	private string? _editingAddress = null;

	private string? _currentFilePath = null;
	private string _statusMessage = string.Empty;
	private bool _hasUnsavedChanges = false;

	// Виконується команда або відкрито діалог: нові команди ігноруються.
	private bool _isBusy = false;

	private bool _isCloseConfirmed = false;

	public MainPage(TableController controller)
	{
		InitializeComponent();
		_controller = controller;

		RebuildGrid();
		RefreshGrid();
		UpdateModeButton();
		UpdateTitle();
		SetStatus("Готово. Клацніть клітинку і введіть вираз.");
	}

	public bool NeedsCloseConfirmation => !_isCloseConfirmed && (_hasUnsavedChanges || HasPendingEdits());

	public async Task<bool> ConfirmCloseAsync()
	{
		await RunCommandAsync(ConfirmQuitAsync);
		return _isCloseConfirmed;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Лише тепер сторінка належить вікну, тож можна оновити його заголовок.
		UpdateTitle();
	}

	#region Обробники меню та кнопок (лише делегують командам)

	private async void OnNewTableClicked(object? sender, EventArgs args) => await RunCommandAsync(CreateNewTableAsync);

	private async void OnOpenClicked(object? sender, EventArgs args) => await RunCommandAsync(OpenTableAsync);

	private async void OnSaveClicked(object? sender, EventArgs args) => await RunCommandAsync(SaveAsync);

	private async void OnSaveAsClicked(object? sender, EventArgs args) => await RunCommandAsync(SaveAsAsync);

	private async void OnExitClicked(object? sender, EventArgs args) => await RunCommandAsync(ExitAsync);

	private async void OnInsertRowClicked(object? sender, EventArgs args) => await RunCommandAsync(InsertRowAsync);

	private async void OnAppendRowClicked(object? sender, EventArgs args) => await RunCommandAsync(AppendRowAsync);

	private async void OnDeleteRowClicked(object? sender, EventArgs args) => await RunCommandAsync(DeleteRowAsync);

	private async void OnInsertColumnClicked(object? sender, EventArgs args)
		=> await RunCommandAsync(InsertColumnAsync);

	private async void OnAppendColumnClicked(object? sender, EventArgs args)
		=> await RunCommandAsync(AppendColumnAsync);

	private async void OnDeleteColumnClicked(object? sender, EventArgs args)
		=> await RunCommandAsync(DeleteColumnAsync);

	private async void OnToggleModeClicked(object? sender, EventArgs args)
		=> await RunCommandAsync(ToggleDisplayModeAsync);

	private async void OnHelpClicked(object? sender, EventArgs args) => await RunCommandAsync(ShowHelpAsync);

	private async void OnAboutClicked(object? sender, EventArgs args) => await RunCommandAsync(ShowAboutAsync);

	#endregion
}
