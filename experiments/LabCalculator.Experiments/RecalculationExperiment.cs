using System.Diagnostics;
using LabCalculator.Core;
using LabCalculator.Core.Models;

namespace LabCalculator.Experiments;

/// <summary>Експеримент 2: перерахунок лише залежних клітинок проти перерахунку всієї таблиці.</summary>
internal static class RecalculationExperiment
{
	private const int Size = 200;
	private const int WarmUpRuns = 3;
	private const int MeasuredRuns = 15;

	public static void Run()
	{
		Console.WriteLine($"Експеримент 2. Зміна A1 у таблиці {Size} × {Size} ({Size * Size} клітинок з формулами)");
		Console.WriteLine();
		Console.WriteLine("| Таблиця | Залежних від A1 | Лише залежні (програма) | Уся таблиця | Різниця |");
		Console.WriteLine("|---|---|---|---|---|");

		Measure("кожен рядок — окремий ланцюжок", isSingleChain: false);
		Measure("уся таблиця — один ланцюжок", isSingleChain: true);
	}

	private static void Measure(string title, bool isSingleChain)
	{
		var storage = new CurrentTableStorage();
		var controller = new TableController(storage);
		storage.Attach(() => controller.Table);
		controller.NewTable(Size, Size);
		Fill(controller, isSingleChain);

		int dependents = isSingleChain ? Size * Size - 1 : Size - 1;
		string lastCell = isSingleChain
			? CellAddress.ToAddress(Size - 1, Size - 1)
			: CellAddress.ToAddress(Size - 1, 0);

		int step = 0;
		double onlyDependents = MedianMilliseconds(() =>
		{
			step++;
			controller.SetCellExpression("A1", step.ToString());
		});
		CheckLastCell(controller, lastCell, step + dependents);

		double wholeTable = MedianMilliseconds(() => controller.Open(string.Empty));
		CheckLastCell(controller, lastCell, step + dependents);

		double ratio = wholeTable / onlyDependents;
		Console.WriteLine($"| {title} | {dependents} | {onlyDependents:F2} мс | {wholeTable:F2} мс | ×{ratio:F1} |");
	}

	private static void Fill(TableController controller, bool isSingleChain)
	{
		for (int row = 0; row < Size; row++)
		{
			for (int column = 0; column < Size; column++)
			{
				string address = CellAddress.ToAddress(column, row);
				string? previous = GetPrevious(column, row, isSingleChain);
				controller.SetCellExpression(address, previous is null ? "1" : previous + " + 1");
			}
		}
	}

	private static string? GetPrevious(int column, int row, bool isSingleChain)
	{
		if (column > 0)
		{
			return CellAddress.ToAddress(column - 1, row);
		}

		bool hasRowAbove = isSingleChain && row > 0;
		return hasRowAbove ? CellAddress.ToAddress(Size - 1, row - 1) : null;
	}

	private static void CheckLastCell(TableController controller, string address, int expected)
	{
		bool isCorrect = controller.Table.GetCell(address).Value == expected;
		if (!isCorrect)
		{
			throw new InvalidOperationException($"Неправильне значення {address}");
		}
	}

	private static double MedianMilliseconds(Action action)
	{
		for (int run = 0; run < WarmUpRuns; run++)
		{
			action();
		}

		var times = new List<double>();
		for (int run = 0; run < MeasuredRuns; run++)
		{
			long start = Stopwatch.GetTimestamp();
			action();
			times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
		}

		times.Sort();
		return times[times.Count / 2];
	}
}
