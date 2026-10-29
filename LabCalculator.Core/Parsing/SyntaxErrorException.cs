namespace LabCalculator.Core.Parsing;

/// <summary>Синтаксична помилка у виразі, яку кидає ThrowExceptionErrorListener.</summary>
internal sealed class SyntaxErrorException : Exception
{
	public SyntaxErrorException(string message, int position)
		: base(message)
	{
		Position = position;
	}

	public int Position { get; }
}
