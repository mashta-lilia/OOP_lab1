using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace LabCalculator.Core.Parsing;

/// <summary>Перевіряє синтаксис виразу (лексер і парсер ANTLR) і знаходить посилання на клітинки.</summary>
public class ExpressionParser
{
	/// <summary>Найбільша довжина виразу: дуже довгий вираз переповнив би стек рекурсивного розбору.</summary>
	public const int MaxExpressionLength = 500;

	private const string TooComplexError = "Вираз занадто складний";

	public ParseResult Parse(string expression)
	{
		bool isBlank = string.IsNullOrWhiteSpace(expression);
		if (isBlank)
		{
			return ParseResult.Failure("Порожній вираз", 0);
		}

		if (expression.Length > MaxExpressionLength)
		{
			return ParseResult.Failure(
				$"Вираз задовгий (максимум {MaxExpressionLength} символів)", MaxExpressionLength);
		}

		return ParseWithoutLengthLimit(expression);
	}

	internal static ParseResult ParseWithoutLengthLimit(string expression)
	{
		// На символах поза основною площиною Unicode (емодзі) лексер ANTLR падає з ArgumentException,
		// тому такі символи відсікаємо до розбору.
		for (int position = 0; position < expression.Length; position++)
		{
			bool isSurrogate = char.IsSurrogate(expression[position]);
			if (isSurrogate)
			{
				string symbol = char.IsSurrogatePair(expression, position)
					? expression.Substring(position, 2)
					: expression[position].ToString();
				return ParseResult.Failure(
					$"Синтаксична помилка в позиції {position + 1}: невідомий символ '{symbol}'", position);
			}
		}

		try
		{
			return RunAntlr(expression);
		}
		catch (InsufficientExecutionStackException)
		{
			return ParseResult.Failure(TooComplexError, 0);
		}
	}

	public IReadOnlyList<string> GetReferences(IParseTree tree)
	{
		var references = new List<string>();

		// Обхід без рекурсії; дочірні вузли кладемо у зворотному порядку, щоб виймати їх зліва направо.
		var nodes = new Stack<IParseTree>();
		nodes.Push(tree);
		while (nodes.Count > 0)
		{
			IParseTree node = nodes.Pop();
			if (node is ITerminalNode terminal)
			{
				string address = terminal.GetText().ToUpperInvariant();
				bool isCell = terminal.Symbol.Type == LabCalculatorLexer.IDENTIFIER;
				bool isNewReference = isCell && !references.Contains(address);
				if (isNewReference)
				{
					references.Add(address);
				}

				continue;
			}

			for (int index = node.ChildCount - 1; index >= 0; index--)
			{
				nodes.Push(node.GetChild(index));
			}
		}

		return references;
	}

	private static ParseResult RunAntlr(string expression)
	{
		var errorListener = new ThrowExceptionErrorListener();

		var lexer = new LabCalculatorLexer(new AntlrInputStream(expression));
		lexer.RemoveErrorListeners();
		lexer.AddErrorListener(errorListener);

		var parser = new LabCalculatorParser(new CommonTokenStream(lexer));
		parser.RemoveErrorListeners();
		parser.AddErrorListener(errorListener);
		parser.AddParseListener(new StackGuard());

		try
		{
			IParseTree tree = parser.compileUnit();
			errorListener.ThrowIfLexerError();
			return ParseResult.Success(tree);
		}
		catch (SyntaxErrorException exception)
		{
			return ParseResult.Failure(
				$"Синтаксична помилка в позиції {exception.Position + 1}: {exception.Message}",
				exception.Position);
		}
	}

	/// <summary>Перед кожним правилом граматики перевіряє, чи вистачає стека парсеру.</summary>
	private sealed class StackGuard : IParseTreeListener
	{
		public void EnterEveryRule(ParserRuleContext context) => RuntimeHelpers.EnsureSufficientExecutionStack();

		public void ExitEveryRule(ParserRuleContext context)
		{
		}

		public void VisitTerminal(ITerminalNode node)
		{
		}

		public void VisitErrorNode(IErrorNode node)
		{
		}
	}
}
