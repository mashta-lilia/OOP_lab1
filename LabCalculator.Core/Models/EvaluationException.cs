namespace LabCalculator.Core.Models;

/// <summary>Помилка обчислення виразу (ділення на 0, від'ємний степінь тощо).</summary>
public class EvaluationException : Exception
{
	public EvaluationException(string message) : base(message)
	{
	}
}
