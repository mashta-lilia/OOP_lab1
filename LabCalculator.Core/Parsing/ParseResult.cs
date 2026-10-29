using Antlr4.Runtime.Tree;

namespace LabCalculator.Core.Parsing;

/// <summary>Результат розбору виразу: дерево або опис першої синтаксичної помилки.</summary>
public class ParseResult
{
	private ParseResult(IParseTree? tree, string? errorMessage, int? errorPosition)
	{
		Tree = tree;
		ErrorMessage = errorMessage;
		ErrorPosition = errorPosition;
	}

	public IParseTree? Tree { get; }

	public string? ErrorMessage { get; }

	public int? ErrorPosition { get; }

	public bool IsSuccess => Tree is not null;

	public static ParseResult Success(IParseTree tree) => new(tree, null, null);

	public static ParseResult Failure(string message, int position) => new(null, message, position);
}
