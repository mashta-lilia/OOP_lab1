using LabCalculator.Core.Models;

namespace LabCalculator.Core.Storage;

/// <summary>Збереження таблиці у файл і читання з файлу (шаблон Strategy).</summary>
public interface ITableStorage
{
	void Save(Table table, string path);

	Table Load(string path);
}
