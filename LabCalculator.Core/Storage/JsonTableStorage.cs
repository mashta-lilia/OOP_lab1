using System.Text.Json;
using LabCalculator.Core.Models;

namespace LabCalculator.Core.Storage;

/// <summary>Збереження таблиці у JSON: розмір і вирази непорожніх клітинок (значення обчислюються заново).</summary>
public class JsonTableStorage : ITableStorage
{
	private const string FormatName = "LabCalculator";

	private const int FormatVersion = 1;

	private static readonly JsonSerializerOptions _options = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

		// Дві однакові адреси клітинок — пошкоджений файл.
		AllowDuplicateProperties = false,
	};

	public void Save(Table table, string path)
	{
		var cells = new Dictionary<string, string?>();
		foreach (string address in table.AllAddresses())
		{
			Cell cell = table.GetCell(address);
			if (!cell.IsEmpty)
			{
				cells[address] = cell.Expression;
			}
		}

		var file = new TableFile(FormatName, FormatVersion, table.RowCount, table.ColumnCount, cells);

		string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
		bool hasDirectory = !string.IsNullOrEmpty(directory);
		if (hasDirectory)
		{
			Directory.CreateDirectory(directory!);
		}

		File.WriteAllText(path, JsonSerializer.Serialize(file, _options));
	}

	public Table Load(string path)
	{
		string json = File.ReadAllText(path);

		TableFile file = DeserializeFile(json);
		CheckHeader(file);

		var table = new Table(file.Rows, file.Columns);

		FillCells(table, file.Cells ?? new Dictionary<string, string?>());
		return table;
	}

	private static TableFile DeserializeFile(string json)
	{
		TableFile? file = null;
		try
		{
			file = JsonSerializer.Deserialize<TableFile>(json, _options);
		}
		catch (JsonException exception)
		{
			throw Corrupted("це не коректний JSON або поля мають неправильний тип", exception);
		}

		if (file is null)
		{
			throw Corrupted("файл не містить даних таблиці");
		}

		return file;
	}

	private static void CheckHeader(TableFile file)
	{
		if (file.Format != FormatName)
		{
			throw Corrupted("це не файл LabCalculator");
		}

		if (file.Version != FormatVersion)
		{
			throw Corrupted($"непідтримувана версія {file.Version}");
		}

		// Та сама межа, що й у Table, тож збережений програмою файл завжди відкриється.
		if (file.Rows < 1 || file.Rows > Table.MaxSize || file.Columns < 1 || file.Columns > Table.MaxSize)
		{
			throw Corrupted($"кількість рядків і стовпчиків має бути від 1 до {Table.MaxSize}");
		}
	}

	private static void FillCells(Table table, Dictionary<string, string?> cells)
	{
		foreach ((string address, string? expression) in cells)
		{
			bool isAddress = CellAddress.TryParse(address, out _, out _);
			if (!isAddress)
			{
				throw Corrupted($"неправильна адреса клітинки '{address}'");
			}

			bool isInsideTable = table.Contains(address);
			if (!isInsideTable)
			{
				throw Corrupted($"клітинка {address} виходить за межі таблиці");
			}

			if (expression is null)
			{
				throw Corrupted($"немає виразу для клітинки {address}");
			}

			table.GetCell(address).SetExpression(expression);
		}
	}

	private static InvalidDataException Corrupted(string reason, Exception? inner = null)
		=> new($"Файл пошкоджений або має неправильний формат: {reason}", inner);

	/// <summary>Вміст JSON-файлу таблиці: формат, версія, розмір і вирази (адреса → вираз).</summary>
	private sealed record TableFile(
		string? Format,
		int Version,
		int Rows,
		int Columns,
		Dictionary<string, string?>? Cells);
}
