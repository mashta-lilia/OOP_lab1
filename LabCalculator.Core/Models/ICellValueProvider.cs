using System.Numerics;

namespace LabCalculator.Core.Models;

/// <summary>Значення клітинок лише для читання (для Evaluator).</summary>
public interface ICellValueProvider
{
	/// <summary>Значення клітинки; порожня клітинка дорівнює 0.</summary>
	BigInteger GetCellValue(string address);
}
