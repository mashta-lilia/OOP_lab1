using System.Numerics;

namespace LabCalculator.Core.Models;

/// <summary>Таблиця клітинок розміром RowCount × ColumnCount; змінювати її може лише LabCalculator.Core.</summary>
public class Table : ICellValueProvider
{
	public const int MaxSize = 200;

	private readonly Dictionary<string, Cell> _cells = new();

	internal Table(int rows, int columns)
	{
		if (rows < 1 || rows > MaxSize)
		{
			throw new ArgumentOutOfRangeException(nameof(rows), $"Кількість рядків має бути від 1 до {MaxSize}");
		}

		if (columns < 1 || columns > MaxSize)
		{
			throw new ArgumentOutOfRangeException(nameof(columns), $"Кількість стовпчиків має бути від 1 до {MaxSize}");
		}

		RowCount = rows;
		ColumnCount = columns;

		foreach (string address in AllAddresses())
		{
			_cells[address] = new Cell();
		}
	}

	public int RowCount { get; private set; }

	public int ColumnCount { get; private set; }

	public Cell GetCell(string address)
	{
		bool found = _cells.TryGetValue(address, out Cell? cell);
		if (!found)
		{
			throw new ArgumentException($"Клітинки {address} немає в таблиці", nameof(address));
		}

		return cell!;
	}

	public bool Contains(string address) => _cells.ContainsKey(address);

	public IEnumerable<string> AllAddresses()
	{
		for (int row = 0; row < RowCount; row++)
		{
			for (int column = 0; column < ColumnCount; column++)
			{
				yield return CellAddress.ToAddress(column, row);
			}
		}
	}

	public BigInteger GetCellValue(string address)
	{
		bool found = _cells.TryGetValue(address, out Cell? cell);
		if (!found)
		{
			throw new EvaluationException($"Клітинка {address} не існує");
		}

		if (cell!.Error != null)
		{
			throw new EvaluationException($"Клітинка {address} містить помилку");
		}

		if (cell.IsEmpty)
		{
			return BigInteger.Zero;
		}

		return cell.Value ?? BigInteger.Zero;
	}

	internal void InsertRow(int row)
	{
		if (RowCount == MaxSize)
		{
			throw new InvalidOperationException($"Таблиця не може мати більше ніж {MaxSize} рядків");
		}

		CheckIndex(row, RowCount, nameof(row));

		// Зсуваємо рядки вниз, починаючи з останнього, щоб нічого не перезаписати.
		for (int source = RowCount - 1; source >= row; source--)
		{
			for (int column = 0; column < ColumnCount; column++)
			{
				MoveCell(column, source, column, source + 1);
			}
		}

		for (int column = 0; column < ColumnCount; column++)
		{
			_cells[CellAddress.ToAddress(column, row)] = new Cell();
		}

		RowCount++;
	}

	internal void DeleteRow(int row)
	{
		if (RowCount == 1)
		{
			throw new InvalidOperationException("У таблиці має залишитися хоча б один рядок");
		}

		CheckIndex(row, RowCount - 1, nameof(row));

		for (int source = row + 1; source < RowCount; source++)
		{
			for (int column = 0; column < ColumnCount; column++)
			{
				MoveCell(column, source, column, source - 1);
			}
		}

		for (int column = 0; column < ColumnCount; column++)
		{
			_cells.Remove(CellAddress.ToAddress(column, RowCount - 1));
		}

		RowCount--;
	}

	internal void InsertColumn(int column)
	{
		if (ColumnCount == MaxSize)
		{
			throw new InvalidOperationException($"Таблиця не може мати більше ніж {MaxSize} стовпчиків");
		}

		CheckIndex(column, ColumnCount, nameof(column));

		for (int source = ColumnCount - 1; source >= column; source--)
		{
			for (int row = 0; row < RowCount; row++)
			{
				MoveCell(source, row, source + 1, row);
			}
		}

		for (int row = 0; row < RowCount; row++)
		{
			_cells[CellAddress.ToAddress(column, row)] = new Cell();
		}

		ColumnCount++;
	}

	internal void DeleteColumn(int column)
	{
		if (ColumnCount == 1)
		{
			throw new InvalidOperationException("У таблиці має залишитися хоча б один стовпчик");
		}

		CheckIndex(column, ColumnCount - 1, nameof(column));

		for (int source = column + 1; source < ColumnCount; source++)
		{
			for (int row = 0; row < RowCount; row++)
			{
				MoveCell(source, row, source - 1, row);
			}
		}

		for (int row = 0; row < RowCount; row++)
		{
			_cells.Remove(CellAddress.ToAddress(ColumnCount - 1, row));
		}

		ColumnCount--;
	}

	private void MoveCell(int fromColumn, int fromRow, int toColumn, int toRow)
	{
		_cells[CellAddress.ToAddress(toColumn, toRow)] = _cells[CellAddress.ToAddress(fromColumn, fromRow)];
	}

	private static void CheckIndex(int index, int maxIndex, string parameterName)
	{
		if (index < 0 || index > maxIndex)
		{
			throw new ArgumentOutOfRangeException(parameterName, $"Індекс має бути від 0 до {maxIndex}");
		}
	}
}
