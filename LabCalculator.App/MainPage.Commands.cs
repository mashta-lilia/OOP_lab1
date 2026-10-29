// Головне вікно, частина 3 з 4: команди меню і кнопок та діалоги.

using LabCalculator.Core;
using LabCalculator.Core.Models;

namespace LabCalculator.App;

public partial class MainPage
{
	#region Команди

	private async Task RunCommandAsync(Func<Task> command)
	{
		if (_isBusy)
		{
			return;
		}

		_isBusy = true;
		try
		{
			bool accepted = CommitPendingEdits();
			if (accepted)
			{
				await command();
			}
			else
			{
				// Незаписаний вираз створив би цикл, тому команду не виконуємо.
				await ShowAlertAsync(CycleTitle, CycleMessage);
			}
		}
		catch (Exception exception)
		{
			// Останній рубіж: неочікувана помилка не повинна закрити програму.
			await ShowAlertAsync(ErrorTitle, GetUnexpectedErrorMessage(exception));
		}
		finally
		{
			_isBusy = false;
		}
	}

	private async Task CreateNewTableAsync()
	{
		bool canContinue = await ConfirmDiscardChangesAsync();
		if (!canContinue)
		{
			return;
		}

		_controller.NewTable(TableController.DefaultRows, TableController.DefaultColumns);
		_currentFilePath = null;
		SetUnsavedChanges(false);
		RebuildGrid();
		RefreshGrid();
		SetStatus("Створено нову таблицю.");
	}

	private async Task OpenTableAsync()
	{
		bool canContinue = await ConfirmDiscardChangesAsync();
		if (!canContinue)
		{
			return;
		}

		var options = new PickOptions
		{
			PickerTitle = "Відкрити таблицю",
			FileTypes = TableFiles.PickerFileType,
		};
		FileResult? file = await FilePicker.Default.PickAsync(options);
		if (file is null)
		{
			return;
		}

		string path = file.FullPath;
		string? error = TableFiles.RunFileOperation(() => _controller.Open(path), path);
		if (error is not null)
		{
			await ShowAlertAsync("Не вдалося відкрити файл", error);
			return;
		}

		_currentFilePath = path;
		SetUnsavedChanges(false);
		RebuildGrid();
		RefreshGrid();
		SetStatus($"Відкрито: {path}");
	}

	private async Task<bool> SaveAsync()
	{
		return _currentFilePath is null
			? await SaveAsAsync()
			: await SaveToFileAsync(_currentFilePath);
	}

	private async Task<bool> SaveAsAsync()
	{
		string suggestedName = _currentFilePath is null
			? TableFiles.DefaultName
			: Path.GetFileNameWithoutExtension(_currentFilePath);

		string? name = await DisplayPromptAsync(
			"Зберегти як",
			$"Назва файлу (його буде збережено в папці {TableFiles.SaveFolder}):",
			"Зберегти",
			AnswerCancel,
			TableFiles.DefaultName,
			initialValue: suggestedName);
		if (name is null)
		{
			return false;
		}

		name = name.Trim();
		bool isValidName = TableFiles.IsValidName(name);
		if (!isValidName)
		{
			await ShowAlertAsync("Неправильна назва", "Назва файлу порожня або містить недопустимі символи.");
			return false;
		}

		string path = TableFiles.BuildSavePath(name);
		bool isOtherExistingFile = File.Exists(path)
			&& !string.Equals(path, _currentFilePath, StringComparison.OrdinalIgnoreCase);
		if (isOtherExistingFile)
		{
			bool replace = await DisplayAlertAsync(
				"Файл уже існує", $"Файл {path} уже існує. Замінити його?", AnswerYes, AnswerNo);
			if (!replace)
			{
				return false;
			}
		}

		return await SaveToFileAsync(path);
	}

	private async Task<bool> SaveToFileAsync(string path)
	{
		string? error = TableFiles.RunFileOperation(() => _controller.Save(path), path);
		if (error is not null)
		{
			await ShowAlertAsync("Не вдалося зберегти файл", error);
			return false;
		}

		_currentFilePath = path;
		SetUnsavedChanges(false);
		SetStatus($"Збережено: {path}");
		return true;
	}

	private async Task ExitAsync()
	{
		await ConfirmQuitAsync();
		if (_isCloseConfirmed)
		{
			Application.Current?.Quit();
		}
	}

	private async Task ConfirmQuitAsync()
	{
		_isCloseConfirmed = await ConfirmDiscardChangesAsync();
	}

	private Task InsertRowAsync()
	{
		int row = GetSelectedPosition()?.Row ?? _controller.Table.RowCount;
		return ChangeTableSizeAsync(() => _controller.InsertRow(row), $"Вставлено рядок {row + 1}.");
	}

	private Task InsertColumnAsync()
	{
		int column = GetSelectedPosition()?.Column ?? _controller.Table.ColumnCount;
		return ChangeTableSizeAsync(
			() => _controller.InsertColumn(column), $"Вставлено стовпчик {CellAddress.ColumnName(column)}.");
	}

	private Task AppendRowAsync()
	{
		int row = _controller.Table.RowCount;
		return ChangeTableSizeAsync(() => _controller.InsertRow(row), $"Додано рядок {row + 1}.");
	}

	private Task AppendColumnAsync()
	{
		int column = _controller.Table.ColumnCount;
		return ChangeTableSizeAsync(
			() => _controller.InsertColumn(column), $"Додано стовпчик {CellAddress.ColumnName(column)}.");
	}

	private async Task DeleteRowAsync()
	{
		Table table = _controller.Table;

		int row = GetSelectedPosition()?.Row ?? table.RowCount - 1;

		// Єдиний рядок не видаляється (контролер пояснить чому), тому тоді не питаємо.
		bool needsConfirmation = table.RowCount > 1 && !IsRowEmpty(row);
		if (needsConfirmation)
		{
			bool confirmed = await ConfirmAsync("Видалення рядка", $"Видалити рядок {row + 1}? {DeleteWarning}");
			if (!confirmed)
			{
				return;
			}
		}

		await ChangeTableSizeAsync(() => _controller.DeleteRow(row), $"Рядок {row + 1} видалено.");
	}

	private async Task DeleteColumnAsync()
	{
		Table table = _controller.Table;
		int column = GetSelectedPosition()?.Column ?? table.ColumnCount - 1;
		string name = CellAddress.ColumnName(column);

		bool needsConfirmation = table.ColumnCount > 1 && !IsColumnEmpty(column);
		if (needsConfirmation)
		{
			bool confirmed = await ConfirmAsync("Видалення стовпчика", $"Видалити стовпчик {name}? {DeleteWarning}");
			if (!confirmed)
			{
				return;
			}
		}

		await ChangeTableSizeAsync(() => _controller.DeleteColumn(column), $"Стовпчик {name} видалено.");
	}

	private (int Column, int Row)? GetSelectedPosition()
	{
		bool isSelected = _selectedAddress is not null && _controller.Table.Contains(_selectedAddress);
		return isSelected ? CellAddress.Parse(_selectedAddress!) : null;
	}

	private async Task ChangeTableSizeAsync(Action change, string doneMessage)
	{
		try
		{
			change();
		}
		catch (InvalidOperationException exception)
		{
			await ShowAlertAsync("Неможливо виконати дію", exception.Message);
			return;
		}

		MarkAsModified();
		UpdateGridSize();
		RefreshGrid();
		SetStatus(doneMessage);
	}

	private Task ToggleDisplayModeAsync()
	{
		_displayMode = _displayMode == DisplayMode.Value ? DisplayMode.Expression : DisplayMode.Value;
		UpdateModeButton();
		RefreshGrid();
		SetStatus(_displayMode == DisplayMode.Value ? "Режим: ЗНАЧЕННЯ." : "Режим: ВИРАЗ.");
		return Task.CompletedTask;
	}

	private Task ShowHelpAsync() => Navigation.PushAsync(new HelpPage());

	private Task ShowAboutAsync() => ShowAlertAsync("Про програму", AboutText);

	private async Task<bool> ConfirmDiscardChangesAsync()
	{
		if (!_hasUnsavedChanges)
		{
			return true;
		}

		string? answer = await DisplayActionSheetAsync(
			"Зберегти зміни?", AnswerCancel, null, AnswerYes, AnswerNo);
		return answer switch
		{
			AnswerYes => await SaveAsync(),
			AnswerNo => true,
			_ => false,
		};
	}

	private bool IsRowEmpty(int row)
	{
		Table table = _controller.Table;
		for (int column = 0; column < table.ColumnCount; column++)
		{
			bool isEmpty = table.GetCell(CellAddress.ToAddress(column, row)).IsEmpty;
			if (!isEmpty)
			{
				return false;
			}
		}

		return true;
	}

	private bool IsColumnEmpty(int column)
	{
		Table table = _controller.Table;
		for (int row = 0; row < table.RowCount; row++)
		{
			bool isEmpty = table.GetCell(CellAddress.ToAddress(column, row)).IsEmpty;
			if (!isEmpty)
			{
				return false;
			}
		}

		return true;
	}

	private Task ShowAlertAsync(string title, string message) => DisplayAlertAsync(title, message, AnswerOk);

	private Task<bool> ConfirmAsync(string title, string message)
		=> DisplayAlertAsync(title, message, AnswerYes, AnswerNo);

	private async Task ShowAlertIfFreeAsync(string title, string message)
	{
		SetStatus(message);
		if (_isBusy)
		{
			return;
		}

		_isBusy = true;
		try
		{
			await ShowAlertAsync(title, message);
		}
		finally
		{
			_isBusy = false;
		}
	}

	private static string GetUnexpectedErrorMessage(Exception exception) =>
		$"Сталася неочікувана помилка, дію не виконано.\nПодробиці: {exception.Message}";

	#endregion
}
