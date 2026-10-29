using System.Globalization;
using System.Numerics;
using LabCalculator.Core;

namespace LabCalculator.Experiments;

/// <summary>Експеримент 1: точність double порівняно з BigInteger, який використовує програма.</summary>
internal static class PrecisionExperiment
{
	private static readonly (string Expression, Func<double> AsDouble)[] _cases =
	[
		("2^53 + 1", () => Math.Pow(2, 53) + 1),
		("2^64 - 1", () => Math.Pow(2, 64) - 1),
		("99999999999999999999 + 1", () => 99999999999999999999.0 + 1),
		("3^40", () => Math.Pow(3, 40)),
		("123456789 * 987654321 * 111", () => 123456789.0 * 987654321.0 * 111),
	];

	public static void Run()
	{
		Console.WriteLine("Експеримент 1. Точність: BigInteger (програма) і double");
		Console.WriteLine();
		Console.WriteLine("| Вираз | Програма (BigInteger) | double | Збігається |");
		Console.WriteLine("|---|---|---|---|");

		var storage = new CurrentTableStorage();
		var controller = new TableController(storage);
		int wrongCount = 0;
		foreach ((string expression, Func<double> asDouble) in _cases)
		{
			controller.SetCellExpression("A1", expression);
			BigInteger exact = controller.Table.GetCell("A1").Value ?? BigInteger.Zero;
			BigInteger fromDouble = new(asDouble());
			bool isEqual = exact == fromDouble;
			if (!isEqual)
			{
				wrongCount++;
			}

			string doubleText = fromDouble.ToString(CultureInfo.InvariantCulture);
			Console.WriteLine($"| `{expression}` | {exact} | {doubleText} | {(isEqual ? "так" : "ні")} |");
		}

		Console.WriteLine();
		Console.WriteLine($"double помилився у {wrongCount} з {_cases.Length} виразів.");
	}
}
