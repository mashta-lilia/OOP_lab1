namespace LabCalculator.App;

/// <summary>Шлях і назва файлів таблиць та повідомлення про помилки роботи з файлами.</summary>
public static class TableFiles
{
	public const string Extension = ".json";

	public const string DefaultName = "таблиця";

	private const string FolderName = "LabCalculator";

	public static FilePickerFileType PickerFileType { get; } = new(
		new Dictionary<DevicePlatform, IEnumerable<string>>
		{
			[DevicePlatform.WinUI] = new[] { Extension },
			[DevicePlatform.Android] = new[] { "application/json" },
			[DevicePlatform.iOS] = new[] { "public.json" },
			[DevicePlatform.MacCatalyst] = new[] { "public.json" },
		});

	public static string SaveFolder =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), FolderName);

	public static bool IsValidName(string name) =>
		name.Length > 0 && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

	public static string BuildSavePath(string name)
	{
		string fileName = name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)
			? name
			: name + Extension;
		return Path.Combine(SaveFolder, fileName);
	}

	/// <summary>Виконує операцію з файлом; повертає null або текст помилки українською.</summary>
	public static string? RunFileOperation(Action fileOperation, string path)
	{
		try
		{
			fileOperation();
			return null;
		}
		catch (InvalidDataException exception)
		{
			return exception.Message;
		}
		catch (IOException exception)
		{
			return GetErrorMessage(exception, path);
		}
		catch (UnauthorizedAccessException exception)
		{
			return GetErrorMessage(exception, path);
		}
	}

	private static string GetErrorMessage(Exception exception, string path) => exception switch
	{
		FileNotFoundException => $"Файл не знайдено: {path}",
		DirectoryNotFoundException => $"Папку не знайдено: {Path.GetDirectoryName(path)}",
		PathTooLongException => $"Шлях до файлу задовгий: {path}",
		UnauthorizedAccessException => $"Немає доступу до файлу: {path}",
		_ => $"Помилка читання/запису файлу: {path}",
	};
}
