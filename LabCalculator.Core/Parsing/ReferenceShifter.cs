using System.Text;
using Antlr4.Runtime;
using LabCalculator.Core.Models;

namespace LabCalculator.Core.Parsing;

/// <summary>Зсуває посилання у виразі після вставлення чи видалення рядка або стовпчика.</summary>
internal sealed class ReferenceShifter
{
	public const string DeletedReference = "#ПОСИЛАННЯ!";

	private readonly bool _isRow;
	private readonly bool _isInsert;
	private readonly int _index;

	private ReferenceShifter(bool isRow, bool isInsert, int index)
	{
		_isRow = isRow;
		_isInsert = isInsert;
		_index = index;
	}

	public static ReferenceShifter RowInserted(int row) => new(true, true, row);

	public static ReferenceShifter RowDeleted(int row) => new(true, false, row);

	public static ReferenceShifter ColumnInserted(int column) => new(false, true, column);

	public static ReferenceShifter ColumnDeleted(int column) => new(false, false, column);

	public string Shift(string expression)
	{
		var result = new StringBuilder(expression.Length);
		int copiedUpTo = 0;

		foreach (IToken token in Tokenize(expression))
		{
			bool isReference = token.Type == LabCalculatorLexer.IDENTIFIER;
			if (!isReference)
			{
				continue;
			}

			string? moved = MoveAddress(token.Text);
			if (moved is null)
			{
				continue;
			}

			result.Append(expression, copiedUpTo, token.StartIndex - copiedUpTo);
			result.Append(moved);
			copiedUpTo = token.StopIndex + 1;
		}

		result.Append(expression, copiedUpTo, expression.Length - copiedUpTo);
		return result.ToString();
	}

	private static IList<IToken> Tokenize(string expression)
	{
		// Лексер падає на емодзі (див. ExpressionParser), тому замінюємо їх на '?' тієї ж довжини.
		char[] safe = expression.Select(symbol => char.IsSurrogate(symbol) ? '?' : symbol).ToArray();

		var lexer = new LabCalculatorLexer(new AntlrInputStream(new string(safe)));
		lexer.RemoveErrorListeners();
		return lexer.GetAllTokens();
	}

	private string? MoveAddress(string reference)
	{
		bool isAddress = CellAddress.TryParse(reference.ToUpperInvariant(), out int column, out int row);
		if (!isAddress)
		{
			return null;
		}

		int position = _isRow ? row : column;
		if (!_isInsert && position == _index)
		{
			return DeletedReference;
		}

		// Найбільший індекс не збільшуємо: таку адресу не можна записати.
		bool isAfterChange = _isInsert ? position >= _index : position > _index;
		if (!isAfterChange || position >= int.MaxValue - 1)
		{
			return null;
		}

		int newPosition = _isInsert ? position + 1 : position - 1;
		return _isRow
			? CellAddress.ToAddress(column, newPosition)
			: CellAddress.ToAddress(newPosition, row);
	}
}
