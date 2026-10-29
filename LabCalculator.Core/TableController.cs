using Antlr4.Runtime.Tree;
using LabCalculator.Core.Dependencies;
using LabCalculator.Core.Models;
using LabCalculator.Core.Parsing;
using LabCalculator.Core.Storage;

namespace LabCalculator.Core;

/// <summary>Контролер (шаблон Controller з GRASP): приймає команди від інтерфейсу.</summary>
public class TableController
{
	public const int DefaultRows = 10;
	public const int DefaultColumns = 6;

	private const string CycleError = "Циклічне посилання";

	private readonly ExpressionParser _parser = new();
	private readonly ITableStorage _storage;

	private DependencyGraph _graph = new();

	// Клітинки з файлу, що замикали цикл: їх немає в графі, після кожної зміни пробуємо додати їх знову.
	private Dictionary<string, IReadOnlyList<string>> _pendingCycleCells = new();

	public TableController(ITableStorage storage)
	{
		_storage = storage;
		Table = new Table(DefaultRows, DefaultColumns);
	}

	public Table Table { get; private set; }

	/// <summary>Записує вираз і перераховує залежні клітинки; false — вираз створив би цикл.</summary>
	public bool SetCellExpression(string address, string text)
	{
		Cell cell = Table.GetCell(address);
		text = (text ?? string.Empty).Trim();
		string? expression = ToParsable(text);

		ParseResult? result = null;
		IReadOnlyList<string> references = [];

		if (expression is null)
		{
			text = string.Empty;
		}
		else
		{
			result = _parser.Parse(expression);
			if (result.IsSuccess)
			{
				references = _parser.GetReferences(result.Tree!);
				bool hasCycle = _graph.WouldCreateCycle(address, references);
				if (hasCycle)
				{
					return false;
				}
			}
		}

		WriteCell(address, cell, text, result, references);
		return true;
	}

	private void WriteCell(
		string address, Cell cell, string text, ParseResult? result, IReadOnlyList<string> references)
	{
		_pendingCycleCells.Remove(address);
		cell.SetExpression(text);

		// Посилання поза таблицею теж потрапляють у граф: клітинку перерахуємо, коли таблиця збільшиться.
		_graph.SetDependencies(address, references);

		if (result is not null && !result.IsSuccess)
		{
			cell.SetError(result.ErrorMessage!);
		}
		else if (result is not null)
		{
			Evaluate(cell, result.Tree!);
		}

		RecalculateDependents(address);
		RetryPendingCycleCells();
	}

	/// <summary>Вставляє рядок перед рядком row (з нуля); row = Table.RowCount додає його в кінець.</summary>
	public void InsertRow(int row) => ChangeStructure(() => Table.InsertRow(row), ReferenceShifter.RowInserted(row));

	/// <summary>Видаляє рядок row (з нуля); посилання на нього стають #ПОСИЛАННЯ!.</summary>
	public void DeleteRow(int row) => ChangeStructure(() => Table.DeleteRow(row), ReferenceShifter.RowDeleted(row));

	public void InsertColumn(int column)
		=> ChangeStructure(() => Table.InsertColumn(column), ReferenceShifter.ColumnInserted(column));

	public void DeleteColumn(int column)
		=> ChangeStructure(() => Table.DeleteColumn(column), ReferenceShifter.ColumnDeleted(column));

	public void NewTable(int rows, int columns)
	{
		Table = new Table(rows, columns);
		_graph = new DependencyGraph();
		_pendingCycleCells = new Dictionary<string, IReadOnlyList<string>>();
	}

	public void Open(string path)
	{
		Table loaded = _storage.Load(path);

		// Якщо обчислення нової таблиці впаде, повертаємо попередній стан.
		Table oldTable = Table;
		DependencyGraph oldGraph = _graph;
		Dictionary<string, IReadOnlyList<string>> oldPending = _pendingCycleCells;

		Table = loaded;
		_graph = new DependencyGraph();
		_pendingCycleCells = new Dictionary<string, IReadOnlyList<string>>();
		try
		{
			RebuildAll();
		}
		catch
		{
			Table = oldTable;
			_graph = oldGraph;
			_pendingCycleCells = oldPending;
			throw;
		}
	}

	public void Save(string path) => _storage.Save(Table, path);

	private static string? ToParsable(string text)
	{
		string trimmed = text.Trim();
		if (trimmed.Length == 0 || trimmed == "=")
		{
			return null;
		}

		return trimmed.StartsWith('=') ? " " + trimmed[1..] : trimmed;
	}

	private void Evaluate(Cell cell, IParseTree tree)
	{
		try
		{
			cell.SetResult(new Evaluator(Table).Evaluate(tree));
		}
		catch (EvaluationException exception)
		{
			cell.SetError(exception.Message);
		}
	}

	private void EvaluateCell(string address)
	{
		Cell cell = Table.GetCell(address);
		string? expression = ToParsable(cell.Expression);
		if (expression is null)
		{
			return;
		}

		ParseResult result = _parser.Parse(expression);
		if (!result.IsSuccess)
		{
			cell.SetError(result.ErrorMessage!);
			return;
		}

		Evaluate(cell, result.Tree!);
	}

	private void RecalculateDependents(string address)
	{
		foreach (string dependent in _graph.GetCellsToRecalculate(address))
		{
			bool exists = Table.Contains(dependent);
			if (exists)
			{
				EvaluateCell(dependent);
			}
		}
	}

	private void ChangeStructure(Action change, ReferenceShifter shifter)
	{
		// Якщо зміна неможлива, Table кине виняток раніше, ніж щось зміниться.
		change();

		foreach (string address in Table.AllAddresses())
		{
			Cell cell = Table.GetCell(address);
			bool isEmpty = cell.IsEmpty;
			if (!isEmpty)
			{
				cell.SetExpression(shifter.Shift(cell.Expression));
			}
		}

		_graph = new DependencyGraph();
		_pendingCycleCells = new Dictionary<string, IReadOnlyList<string>>();
		RebuildAll();
	}

	private void RetryPendingCycleCells()
	{
		// ToList — бо під час обходу видаляємо зі словника.
		foreach ((string address, IReadOnlyList<string> references) in _pendingCycleCells.ToList())
		{
			bool hasCycle = _graph.WouldCreateCycle(address, references);
			if (hasCycle)
			{
				continue;
			}

			_pendingCycleCells.Remove(address);
			_graph.SetDependencies(address, references);
			EvaluateCell(address);
			RecalculateDependents(address);
		}
	}

	private void RebuildAll()
	{
		var trees = new Dictionary<string, IParseTree>();

		foreach (string address in Table.AllAddresses())
		{
			AddLoadedCell(address, trees);
		}

		foreach (string address in _graph.GetEvaluationOrder(trees.Keys))
		{
			Evaluate(Table.GetCell(address), trees[address]);
		}
	}

	private void AddLoadedCell(string address, Dictionary<string, IParseTree> trees)
	{
		Cell cell = Table.GetCell(address);

		string text = cell.Expression.Trim();
		string? expression = ToParsable(text);
		if (expression is null)
		{
			cell.SetExpression(string.Empty);
			return;
		}

		cell.SetExpression(text);
		ParseResult result = _parser.Parse(expression);
		if (!result.IsSuccess)
		{
			cell.SetError(result.ErrorMessage!);
			return;
		}

		IReadOnlyList<string> references = _parser.GetReferences(result.Tree!);
		bool hasCycle = _graph.WouldCreateCycle(address, references);
		if (hasCycle)
		{
			// Клітинку, що замикає цикл, не додаємо в граф, щоб він залишався без циклів.
			cell.SetError(CycleError);
			_pendingCycleCells[address] = references;
			return;
		}

		_graph.SetDependencies(address, references);
		trees[address] = result.Tree!;
	}
}
