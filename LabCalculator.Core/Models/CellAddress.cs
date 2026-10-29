namespace LabCalculator.Core.Models;

/// <summary>Адреси клітинок: "A1" ↔ (стовпчик 0, рядок 0); індекси — з нуля.</summary>
public static class CellAddress
{
	private const int LettersCount = 26;

	private const int DecimalBase = 10;

	public static (int Column, int Row) Parse(string address)
	{
		bool isValid = TryParse(address, out int column, out int row);
		if (!isValid)
		{
			throw new FormatException($"Неправильна адреса клітинки: '{address}'");
		}

		return (column, row);
	}

	public static bool TryParse(string address, out int column, out int row)
	{
		column = 0;
		row = 0;

		bool isEmpty = string.IsNullOrEmpty(address);
		if (isEmpty)
		{
			return false;
		}

		int lettersEnd = CountColumnLetters(address);

		// Має бути хоча б одна літера і хоча б одна цифра; номер рядка не може починатися з 0.
		if (lettersEnd == 0 || lettersEnd == address.Length || address[lettersEnd] == '0')
		{
			return false;
		}

		bool isColumnValid = TryReadColumnNumber(address, lettersEnd, out long columnNumber);
		if (!isColumnValid)
		{
			return false;
		}

		bool isRowValid = TryReadRowNumber(address, lettersEnd, out long rowNumber);
		if (!isRowValid)
		{
			return false;
		}

		column = (int)columnNumber - 1;
		row = (int)rowNumber - 1;
		return true;
	}

	public static string ToAddress(int column, int row)
	{
		if (row < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(row), "Номер рядка не може бути від'ємним");
		}

		// Номер рядка в адресі на 1 більший за індекс і має вміститися в int (див. TryParse).
		if (row == int.MaxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(row), "Номер рядка завеликий");
		}

		return ColumnName(column) + (row + 1);
	}

	public static string ColumnName(int column)
	{
		if (column < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(column), "Номер стовпчика не може бути від'ємним");
		}

		if (column == int.MaxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(column), "Номер стовпчика завеликий");
		}

		int number = column + 1;
		var letters = new Stack<char>();
		while (number > 0)
		{
			number--;
			letters.Push((char)('A' + number % LettersCount));
			number /= LettersCount;
		}

		return new string(letters.ToArray());
	}

	private static int CountColumnLetters(string address)
	{
		int lettersEnd = 0;
		while (lettersEnd < address.Length && address[lettersEnd] >= 'A' && address[lettersEnd] <= 'Z')
		{
			lettersEnd++;
		}

		return lettersEnd;
	}

	private static bool TryReadColumnNumber(string address, int lettersEnd, out long columnNumber)
	{
		columnNumber = 0;
		for (int position = 0; position < lettersEnd; position++)
		{
			columnNumber = columnNumber * LettersCount + (address[position] - 'A' + 1);
			if (columnNumber > int.MaxValue)
			{
				return false;
			}
		}

		return true;
	}

	private static bool TryReadRowNumber(string address, int lettersEnd, out long rowNumber)
	{
		rowNumber = 0;
		for (int position = lettersEnd; position < address.Length; position++)
		{
			char symbol = address[position];
			if (symbol < '0' || symbol > '9')
			{
				return false;
			}

			rowNumber = rowNumber * DecimalBase + (symbol - '0');
			if (rowNumber > int.MaxValue)
			{
				return false;
			}
		}

		return true;
	}
}
