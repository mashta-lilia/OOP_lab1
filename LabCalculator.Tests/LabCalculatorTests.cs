using System.Numerics;
using LabCalculator.Core;
using LabCalculator.Core.Models;
using LabCalculator.Core.Parsing;
using LabCalculator.Core.Storage;
using Moq;

namespace LabCalculator.Tests;

/// <summary>Unit-тести ЛР1; залежності замінено моками Moq.</summary>
public class LabCalculatorTests
{
	private readonly ExpressionParser _parser = new();

	private BigInteger Evaluate(string expression, ICellValueProvider cells)
	{
		ParseResult result = _parser.Parse(expression);
		Assert.True(result.IsSuccess, result.ErrorMessage);
		return new Evaluator(cells).Evaluate(result.Tree!);
	}

	[Fact]
	public void Parse_ValidExpression_FindsCellReferences()
	{
		ParseResult result = _parser.Parse("inc(A1) - B2^2");

		Assert.True(result.IsSuccess);
		Assert.Equal(["A1", "B2"], _parser.GetReferences(result.Tree!));
	}

	[Fact]
	public void Parse_SyntaxError_ReportsPosition()
	{
		ParseResult result = _parser.Parse("2 + * 3");

		Assert.False(result.IsSuccess);
		Assert.Equal(4, result.ErrorPosition);
		Assert.StartsWith("Синтаксична помилка в позиції 5", result.ErrorMessage);
	}

	[Fact]
	public void Evaluate_Variant8Operations_RespectPriority()
	{
		// Strict: будь-яке звернення до клітинок у цих виразах — помилка тесту.
		var cells = new Mock<ICellValueProvider>(MockBehavior.Strict);

		Assert.Equal(14, Evaluate("2+3*4", cells.Object));
		Assert.Equal(512, Evaluate("2^3^2", cells.Object));

		// Унарний мінус сильніший за ^: (-2)^2.
		Assert.Equal(4, Evaluate("-2^2", cells.Object));
		Assert.Equal(-4, Evaluate("-(2^2)", cells.Object));
		Assert.Equal(6, Evaluate("inc(5)", cells.Object));
		Assert.Equal(-1, Evaluate("dec(0)", cells.Object));
		Assert.Equal(-3, Evaluate("-7/2", cells.Object));

		Assert.Equal(BigInteger.Parse("100000000000000000000"), Evaluate("99999999999999999999 + 1", cells.Object));
	}

	[Fact]
	public void Evaluate_DivisionByZero_ThrowsEvaluationException()
	{
		var cells = new Mock<ICellValueProvider>(MockBehavior.Strict);

		EvaluationException exception = Assert.Throws<EvaluationException>(() => Evaluate("10 / 0", cells.Object));

		Assert.Equal("Ділення на нуль", exception.Message);
	}

	[Fact]
	public void Evaluate_CellReference_ReadsValueFromProvider()
	{
		var cells = new Mock<ICellValueProvider>();
		cells.Setup(provider => provider.GetCellValue("A1")).Returns(5);

		// a1 і A1 — та сама клітинка.
		BigInteger value = Evaluate("a1 * 2", cells.Object);

		Assert.Equal(10, value);
		cells.Verify(provider => provider.GetCellValue("A1"), Times.Once());
	}

	[Fact]
	public void Evaluate_ProviderError_IsPropagated()
	{
		var cells = new Mock<ICellValueProvider>();
		cells.Setup(provider => provider.GetCellValue("B1"))
			.Throws(new EvaluationException("Клітинка B1 містить помилку"));

		EvaluationException exception = Assert.Throws<EvaluationException>(() => Evaluate("B1 + 1", cells.Object));

		Assert.Equal("Клітинка B1 містить помилку", exception.Message);
	}

	[Fact]
	public void SetCellExpression_ChangedCell_RecalculatesDependents()
	{
		var storage = new Mock<ITableStorage>();
		var controller = new TableController(storage.Object);
		controller.SetCellExpression("A1", "5");
		controller.SetCellExpression("B1", "A1 + 1");
		Assert.Equal(6, controller.Table.GetCell("B1").Value);

		controller.SetCellExpression("A1", "10");

		Assert.Equal(11, controller.Table.GetCell("B1").Value);

		storage.VerifyNoOtherCalls();
	}

	[Fact]
	public void SetCellExpression_CircularReference_IsRejected()
	{
		var controller = new TableController(new Mock<ITableStorage>().Object);
		controller.SetCellExpression("A1", "B1");

		bool accepted = controller.SetCellExpression("B1", "A1");

		Assert.False(accepted);
		Assert.True(controller.Table.GetCell("B1").IsEmpty);
		Assert.Equal(0, controller.Table.GetCell("A1").Value);
	}

	[Fact]
	public void DeleteRow_ShiftsReferencesAndMarksDeletedOnes()
	{
		var storage = new Mock<ITableStorage>();
		var controller = new TableController(storage.Object);
		controller.SetCellExpression("A1", "5");
		controller.SetCellExpression("A2", "7");
		controller.SetCellExpression("B3", "A2 * 2");
		controller.SetCellExpression("B4", "A1 + A2");

		// Індекс 0 — це рядок 1.
		controller.DeleteRow(0);

		Table table = controller.Table;

		Assert.Equal("7", table.GetCell("A1").Expression);
		Assert.Equal("A1 * 2", table.GetCell("B2").Expression);
		Assert.Equal(14, table.GetCell("B2").Value);
		Assert.Equal("#ПОСИЛАННЯ! + A1", table.GetCell("B3").Expression);
		Assert.Equal("Посилання на видалену клітинку", table.GetCell("B3").Error);
		storage.VerifyNoOtherCalls();
	}

	[Fact]
	public void OpenAndSave_GoThroughStorage()
	{
		var loaded = new Table(2, 2);
		loaded.GetCell("A1").SetExpression("5");
		loaded.GetCell("B1").SetExpression("A1 * 2");
		var storage = new Mock<ITableStorage>();
		storage.Setup(tableStorage => tableStorage.Load("in.json")).Returns(loaded);
		var controller = new TableController(storage.Object);

		controller.Open("in.json");
		controller.Save("out.json");

		Assert.Equal(10, controller.Table.GetCell("B1").Value);
		storage.Verify(tableStorage => tableStorage.Load("in.json"), Times.Once());
		storage.Verify(tableStorage => tableStorage.Save(loaded, "out.json"), Times.Once());
	}
}
