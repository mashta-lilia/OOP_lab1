using LabCalculator.Core.Models;
using LabCalculator.Core.Storage;

namespace LabCalculator.Experiments;

/// <summary>Сховище без файлів: Open перераховує поточну таблицю, як після відкриття файлу.</summary>
internal sealed class CurrentTableStorage : ITableStorage
{
	private Func<Table>? _currentTable = null;

	public void Attach(Func<Table> currentTable)
	{
		_currentTable = currentTable;
	}

	public void Save(Table table, string path)
	{
	}

	public Table Load(string path)
	{
		if (_currentTable is null)
		{
			throw new InvalidOperationException("Таблицю ще не під'єднано");
		}

		return _currentTable();
	}
}
