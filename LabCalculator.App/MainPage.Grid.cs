// Головне вікно, частина 4 з 4: побудова сітки і відображення клітинок, рядка формул і стану.

using LabCalculator.Core.Models;

// У MAUI теж є клас Cell, тому явно вказуємо клітинку нашої таблиці.
using Cell = LabCalculator.Core.Models.Cell;

namespace LabCalculator.App;

public partial class MainPage
{
	#region Побудова сітки

	private void RebuildGrid()
	{
		// Запізнілі Focused/Unfocused забутих полів TryGetCellEntry проігнорує.
		_entriesByAddress.Clear();
		_addressesByEntry.Clear();
		_renderedStates.Clear();
		_rowHeaders.Clear();
		_columnHeaders.Clear();
		_selectedAddress = null;
		_editingAddress = null;

		grdTable.Children.Clear();
		grdTable.RowDefinitions.Clear();
		grdTable.ColumnDefinitions.Clear();

		grdTable.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(HeaderColumnWidth)));
		grdTable.RowDefinitions.Add(new RowDefinition(new GridLength(RowHeight)));
		grdTable.Add(CreateHeaderLabel(string.Empty), 0, 0);

		UpdateGridSize();
	}

	private void UpdateGridSize()
	{
		Table table = _controller.Table;
		while (_columnHeaders.Count < table.ColumnCount)
		{
			AddGridColumn();
		}

		while (_columnHeaders.Count > table.ColumnCount)
		{
			RemoveGridColumn();
		}

		while (_rowHeaders.Count < table.RowCount)
		{
			AddGridRow();
		}

		while (_rowHeaders.Count > table.RowCount)
		{
			RemoveGridRow();
		}

		bool isSelectedRemoved = _selectedAddress is not null && !table.Contains(_selectedAddress);
		if (isSelectedRemoved)
		{
			_selectedAddress = null;
		}

		bool isEditingRemoved = _editingAddress is not null && !table.Contains(_editingAddress);
		if (isEditingRemoved)
		{
			_editingAddress = null;
		}
	}

	private void AddGridRow()
	{
		int row = _rowHeaders.Count;
		grdTable.RowDefinitions.Add(new RowDefinition(new GridLength(RowHeight)));

		Label header = CreateHeaderLabel((row + 1).ToString());
		grdTable.Add(header, 0, row + 1);
		_rowHeaders.Add(header);

		for (int column = 0; column < _columnHeaders.Count; column++)
		{
			AddCellEntry(column, row);
		}
	}

	private void AddGridColumn()
	{
		int column = _columnHeaders.Count;
		grdTable.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(CellWidth)));

		Label header = CreateHeaderLabel(CellAddress.ColumnName(column));
		grdTable.Add(header, column + 1, 0);
		_columnHeaders.Add(header);

		for (int row = 0; row < _rowHeaders.Count; row++)
		{
			AddCellEntry(column, row);
		}
	}

	private void RemoveGridRow()
	{
		int row = _rowHeaders.Count - 1;
		for (int column = 0; column < _columnHeaders.Count; column++)
		{
			RemoveCellEntry(CellAddress.ToAddress(column, row));
		}

		grdTable.Remove(_rowHeaders[row]);
		_rowHeaders.RemoveAt(row);
		grdTable.RowDefinitions.RemoveAt(row + 1);
	}

	private void RemoveGridColumn()
	{
		int column = _columnHeaders.Count - 1;
		for (int row = 0; row < _rowHeaders.Count; row++)
		{
			RemoveCellEntry(CellAddress.ToAddress(column, row));
		}

		grdTable.Remove(_columnHeaders[column]);
		_columnHeaders.RemoveAt(column);
		grdTable.ColumnDefinitions.RemoveAt(column + 1);
	}

	private void AddCellEntry(int column, int row)
	{
		string address = CellAddress.ToAddress(column, row);
		Entry entry = CreateCellEntry();
		_entriesByAddress[address] = entry;
		_addressesByEntry[entry] = address;
		grdTable.Add(entry, column + 1, row + 1);
	}

	private void RemoveCellEntry(string address)
	{
		Entry entry = _entriesByAddress[address];

		// Спершу прибираємо зі словників: запізнілий Unfocused видаленого поля буде проігноровано.
		_entriesByAddress.Remove(address);
		_addressesByEntry.Remove(entry);
		_renderedStates.Remove(entry);
		grdTable.Remove(entry);
	}

	private static Label CreateHeaderLabel(string text)
	{
		var label = new Label
		{
			Text = text,
			FontAttributes = FontAttributes.Bold,
			HorizontalTextAlignment = TextAlignment.Center,
			VerticalTextAlignment = TextAlignment.Center,
		};
		label.SetAppThemeColor(VisualElement.BackgroundColorProperty, _headerBackgroundLight, _headerBackgroundDark);
		return label;
	}

	private Entry CreateCellEntry()
	{
		var entry = new Entry
		{
			FontSize = CellFontSize,
			MinimumHeightRequest = 0,
			MinimumWidthRequest = 0,
			VerticalTextAlignment = TextAlignment.Center,
			ClearButtonVisibility = ClearButtonVisibility.Never,
			IsSpellCheckEnabled = false,
			IsTextPredictionEnabled = false,
		};
		entry.Focused += OnCellFocused;
		entry.Unfocused += OnCellUnfocused;
		entry.Completed += OnCellCompleted;
		return entry;
	}

	private bool TryGetCellEntry(object? sender, out Entry entry, out string address)
	{
		if (sender is Entry senderEntry)
		{
			bool isKnown = _addressesByEntry.TryGetValue(senderEntry, out string? found);
			if (isKnown)
			{
				entry = senderEntry;
				address = found!;
				return true;
			}
		}

		entry = null!;
		address = string.Empty;
		return false;
	}

	#endregion

	#region Відображення

	private void RefreshGrid()
	{
		foreach (KeyValuePair<string, Entry> pair in _entriesByAddress)
		{
			RenderCell(pair.Key, pair.Value);
		}

		UpdateFormulaBar();
		UpdateStatusBar();
	}

	private void RenderCellIfShown(string? address)
	{
		if (address is null)
		{
			return;
		}

		bool isShown = _entriesByAddress.TryGetValue(address, out Entry? entry);
		if (isShown)
		{
			RenderCell(address, entry!);
		}
	}

	private void RenderCell(string address, Entry entry)
	{
		Cell cell = _controller.Table.GetCell(address);
		string? fullValue = GetFullValueText(cell);

		string text = GetCellText(address, cell, fullValue);
		if (entry.Text != text)
		{
			entry.Text = text;
		}

		var state = new RenderedState(cell.Error is not null, address == _selectedAddress, fullValue);
		bool isNew = !_renderedStates.TryGetValue(entry, out RenderedState shown);

		if (isNew || shown.HasError != state.HasError)
		{
			entry.SetAppThemeColor(
				Entry.TextColorProperty,
				state.HasError ? _errorTextLight : _cellTextLight,
				state.HasError ? _errorTextDark : _cellTextDark);
		}

		if (isNew || shown.IsSelected != state.IsSelected)
		{
			entry.SetAppThemeColor(
				VisualElement.BackgroundColorProperty,
				state.IsSelected ? _selectedBackgroundLight : _cellBackgroundLight,
				state.IsSelected ? _selectedBackgroundDark : _cellBackgroundDark);
		}

		if (isNew || shown.ToolTip != state.ToolTip)
		{
			if (state.ToolTip is null)
			{
				entry.ClearValue(ToolTipProperties.TextProperty);
			}
			else
			{
				ToolTipProperties.SetText(entry, state.ToolTip);
			}
		}

		_renderedStates[entry] = state;
	}

	private string GetCellText(string address, Cell cell, string? fullValue)
	{
		if (address == _editingAddress || _displayMode == DisplayMode.Expression)
		{
			return cell.Expression;
		}

		return cell.Error is null ? fullValue ?? string.Empty : ErrorMarker;
	}

	private static string? GetFullValueText(Cell cell) => cell.Error ?? cell.Value?.ToString();

	private Cell? GetSelectedCell()
	{
		Table table = _controller.Table;
		return _selectedAddress is not null && table.Contains(_selectedAddress)
			? table.GetCell(_selectedAddress)
			: null;
	}

	private void UpdateFormulaBar()
	{
		Cell? cell = GetSelectedCell();
		lblSelectedAddress.Text = cell is null ? NoSelectionText : _selectedAddress;
		txtFormula.Text = cell?.Expression ?? string.Empty;
		txtFormula.IsEnabled = cell is not null;

		bool hasError = cell?.Error is not null;
		txtValue.Text = cell is null ? string.Empty : GetFullValueText(cell) ?? string.Empty;
		txtValue.SetAppThemeColor(
			Entry.TextColorProperty,
			hasError ? _errorTextLight : _cellTextLight,
			hasError ? _errorTextDark : _cellTextDark);
	}

	private void UpdateStatusBar()
	{
		string? error = GetSelectedCell()?.Error;
		lblStatus.Text = error is null ? _statusMessage : $"{_selectedAddress}: {error}";
		if (error is null)
		{
			lblStatus.SetAppThemeColor(Label.TextColorProperty, _cellTextLight, _cellTextDark);
		}
		else
		{
			lblStatus.SetAppThemeColor(Label.TextColorProperty, _errorTextLight, _errorTextDark);
		}
	}

	private void SetStatus(string message)
	{
		_statusMessage = message;
		UpdateStatusBar();
	}

	private void UpdateModeButton()
	{
		bool isValueMode = _displayMode == DisplayMode.Value;
		btnMode.Text = isValueMode ? ModeValueText : ModeExpressionText;
		ToolTipProperties.SetText(btnMode, isValueMode ? SwitchToExpressionHint : SwitchToValueHint);
	}

	private void MarkAsModified()
	{
		// Після нової зміни попередня згода закрити програму вже не діє.
		_isCloseConfirmed = false;
		SetUnsavedChanges(true);
	}

	private void SetUnsavedChanges(bool value)
	{
		_hasUnsavedChanges = value;
		UpdateTitle();
	}

	private void UpdateTitle()
	{
		string fileName = _currentFilePath is null ? NewTableName : Path.GetFileName(_currentFilePath);
		string title = $"{fileName}{(_hasUnsavedChanges ? " *" : string.Empty)} — {AppTitle}";

		Title = title;
		if (Window is not null)
		{
			Window.Title = title;
		}
	}

	#endregion

	/// <summary>Що вже показано в полі клітинки: колір помилки, виділення і текст підказки.</summary>
	private readonly record struct RenderedState(bool HasError, bool IsSelected, string? ToolTip);
}
