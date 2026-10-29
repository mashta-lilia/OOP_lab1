// Головне вікно, частина 2 з 4: редагування клітинок і рядка формул.

using LabCalculator.Core.Models;

// У MAUI теж є клас Cell, тому явно вказуємо клітинку нашої таблиці.
using Cell = LabCalculator.Core.Models.Cell;

namespace LabCalculator.App;

public partial class MainPage
{
	#region Обробники клітинок і рядка формул

	private async void OnCellFocused(object? sender, FocusEventArgs args)
	{
		bool isCell = TryGetCellEntry(sender, out Entry entry, out string address);
		if (isCell && address != _editingAddress)
		{
			await RunEditorActionAsync(() => StartEditing(address, entry));
		}
	}

	private async void OnCellUnfocused(object? sender, FocusEventArgs args)
	{
		// Якщо Focused іншої клітинки надійшов раніше, ця клітинка вже записана.
		bool isCell = TryGetCellEntry(sender, out _, out string address);
		if (isCell && address == _editingAddress)
		{
			await RunEditorActionAsync(StopEditing);
		}
	}

	private async void OnCellCompleted(object? sender, EventArgs args)
	{
		bool isCell = TryGetCellEntry(sender, out _, out string address);
		if (isCell && address == _editingAddress)
		{
			await RunEditorActionAsync(CommitEditingCell);
		}
	}

	private async void OnFormulaCompleted(object? sender, EventArgs args)
		=> await RunEditorActionAsync(CommitFormulaBar);

	private async void OnFormulaUnfocused(object? sender, FocusEventArgs args)
		=> await RunEditorActionAsync(CommitFormulaBar);

	#endregion

	#region Редагування клітинок

	private bool StartEditing(string address, Entry entry)
	{
		// Unfocused попередньої клітинки може надійти пізніше, тому незаписаний текст записуємо зараз.
		bool accepted = CommitPendingEdits();

		string? previousAddress = _selectedAddress;
		_selectedAddress = address;
		_editingAddress = address;

		RenderCellIfShown(previousAddress);
		RenderCell(address, entry);
		entry.CursorPosition = entry.Text?.Length ?? 0;
		UpdateFormulaBar();
		UpdateStatusBar();
		return accepted;
	}

	private bool CommitEditingCell()
	{
		string? address = _editingAddress;
		if (address is null)
		{
			return true;
		}

		bool isShown = _entriesByAddress.TryGetValue(address, out Entry? entry);
		if (isShown)
		{
			return ApplyExpression(address, entry!.Text ?? string.Empty);
		}

		return true;
	}

	private bool StopEditing()
	{
		bool accepted = CommitEditingCell();

		string? address = _editingAddress;
		_editingAddress = null;
		RenderCellIfShown(address);
		return accepted;
	}

	private bool CommitFormulaBar()
	{
		if (_selectedAddress is string address && GetPendingFormulaText() is string text)
		{
			return ApplyExpression(address, text);
		}

		return true;
	}

	private bool CommitPendingEdits()
	{
		// Текст рядка формул читаємо заздалегідь: запис клітинки оновлює рядок формул і стер би його.
		string? formulaText = GetPendingFormulaText();
		string? formulaAddress = _selectedAddress;

		bool accepted = CommitEditingCell();
		if (formulaText is not null && formulaAddress is not null)
		{
			accepted = ApplyExpression(formulaAddress, formulaText) && accepted;
		}

		return accepted;
	}

	private bool HasPendingEdits()
	{
		if (GetPendingFormulaText() is not null)
		{
			return true;
		}

		return _editingAddress is string address
			&& _entriesByAddress.TryGetValue(address, out Entry? entry)
			&& (entry.Text ?? string.Empty) != _controller.Table.GetCell(address).Expression;
	}

	private string? GetPendingFormulaText()
	{
		Cell? cell = GetSelectedCell();
		string text = txtFormula.Text ?? string.Empty;
		return cell is not null && text != cell.Expression ? text : null;
	}

	private bool ApplyExpression(string address, string text)
	{
		Table table = _controller.Table;
		bool exists = table.Contains(address);
		if (!exists)
		{
			return true;
		}

		bool isUnchanged = text == table.GetCell(address).Expression;
		if (isUnchanged)
		{
			return true;
		}

		bool accepted = _controller.SetCellExpression(address, text);
		if (accepted)
		{
			MarkAsModified();
		}

		RefreshGrid();
		return accepted;
	}

	private async Task RunEditorActionAsync(Func<bool> commit)
	{
		try
		{
			bool accepted = commit();
			if (!accepted)
			{
				await ShowAlertIfFreeAsync(CycleTitle, CycleMessage);
			}
		}
		catch (Exception exception)
		{
			// Останній рубіж: неочікувана помилка не повинна закрити програму.
			await ShowAlertIfFreeAsync(ErrorTitle, GetUnexpectedErrorMessage(exception));
		}
	}

	#endregion
}
