using System.Numerics;

namespace LabCalculator.Core.Models;

/// <summary>Клітинка: вираз, обчислене значення або помилка.</summary>
public class Cell
{
	internal Cell()
	{
	}

	public string Expression { get; private set; } = string.Empty;

	public BigInteger? Value { get; private set; } = null;

	public string? Error { get; private set; } = null;

	public bool IsEmpty => string.IsNullOrWhiteSpace(Expression);

	internal void SetExpression(string text)
	{
		Expression = text ?? string.Empty;
		Value = null;
		Error = null;
	}

	internal void SetResult(BigInteger value)
	{
		Value = value;
		Error = null;
	}

	internal void SetError(string message)
	{
		Error = message;
		Value = null;
	}
}
