using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace LabCalculator.Core.Parsing;

/// <summary>Обробник помилок ANTLR: кидає SyntaxErrorException з першою помилкою у виразі.</summary>
internal sealed class ThrowExceptionErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
	// Помилку лексера не кидаємо одразу: він читає наперед, і парсер може знайти помилку раніше у виразі.
	private SyntaxErrorException? _lexerError = null;

	public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
		int line, int charPositionInLine, string message, RecognitionException exception)
	{
		string detail = offendingSymbol.Type == TokenConstants.EOF
			? "неочікуваний кінець виразу"
			: $"неочікуваний символ '{offendingSymbol.Text}'";

		var parserError = new SyntaxErrorException(detail, offendingSymbol.StartIndex);
		bool isLexerErrorFirst = _lexerError is not null && _lexerError.Position < parserError.Position;
		throw isLexerErrorFirst ? _lexerError! : parserError;
	}

	public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
		int line, int charPositionInLine, string message, RecognitionException exception)
	{
		var lexer = (Lexer)recognizer;
		int start = lexer.TokenStartCharIndex;
		var input = (ICharStream)lexer.InputStream;
		string symbol = input.GetText(Interval.Of(start, start));

		_lexerError ??= new SyntaxErrorException($"невідомий символ '{symbol}'", start);
	}

	public void ThrowIfLexerError()
	{
		if (_lexerError is not null)
		{
			throw _lexerError;
		}
	}
}
