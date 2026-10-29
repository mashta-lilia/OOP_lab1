using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Antlr4.Runtime.Tree;
using LabCalculator.Core.Models;

namespace LabCalculator.Core.Parsing;

/// <summary>Обчислює дерево виразу (шаблон Visitor); значення клітинок читає через ICellValueProvider.</summary>
public class Evaluator : LabCalculatorBaseVisitor<BigInteger>
{
	private const int MaxExponent = 10000;

	public const int MaxResultBits = 100_000;

	private readonly ICellValueProvider _cells;

	public Evaluator(ICellValueProvider cells)
	{
		_cells = cells;
	}

	public BigInteger Evaluate(IParseTree tree)
	{
		ArgumentNullException.ThrowIfNull(tree);
		return Visit(tree);
	}

	/// <summary>Перед кожним рекурсивним викликом перевіряє, чи вистачає стека.</summary>
	public override BigInteger Visit(IParseTree tree)
	{
		try
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
		}
		catch (InsufficientExecutionStackException)
		{
			throw new EvaluationException("Вираз занадто складний");
		}

		return base.Visit(tree);
	}

	// Усі вузли перевизначено явно: стандартний VisitChildren повернув би результат
	// останнього дочірнього вузла (наприклад, EOF), а не значення виразу.

	public override BigInteger VisitCompileUnit(LabCalculatorParser.CompileUnitContext context)
		=> Visit(context.expression());

	public override BigInteger VisitParenthesizedExpr(LabCalculatorParser.ParenthesizedExprContext context)
		=> Visit(context.expression());

	public override BigInteger VisitIncDecExpr(LabCalculatorParser.IncDecExprContext context)
	{
		BigInteger value = Visit(context.expression());
		return CheckSize(context.operatorToken.Type == LabCalculatorLexer.INC ? value + 1 : value - 1);
	}

	public override BigInteger VisitExponentialExpr(LabCalculatorParser.ExponentialExprContext context)
	{
		BigInteger baseValue = WalkLeft(context);
		BigInteger exponent = WalkRight(context);

		if (exponent < 0)
		{
			throw new EvaluationException("Від'ємний показник степеня");
		}

		if (exponent > MaxExponent)
		{
			throw new EvaluationException($"Показник степеня завеликий (максимум {MaxExponent})");
		}

		// Результат має щонайменше (b - 1) * exponent + 1 біт, де b — довжина основи.
		// Якщо навіть це більше за межу, не обчислюємо степінь зовсім.
		long minResultBits = (BitLength(baseValue) - 1) * (long)exponent + 1;
		if (minResultBits > MaxResultBits)
		{
			throw TooBigResult();
		}

		return CheckSize(BigInteger.Pow(baseValue, (int)exponent));
	}

	public override BigInteger VisitUnaryExpr(LabCalculatorParser.UnaryExprContext context)
	{
		BigInteger value = Visit(context.expression());
		return context.operatorToken.Type == LabCalculatorLexer.SUBTRACT ? -value : value;
	}

	public override BigInteger VisitMultiplicativeExpr(LabCalculatorParser.MultiplicativeExprContext context)
	{
		BigInteger left = WalkLeft(context);
		BigInteger right = WalkRight(context);

		if (context.operatorToken.Type == LabCalculatorLexer.MULTIPLY)
		{
			// Добуток чисел з a і b біт має щонайменше a + b - 1 біт.
			if (BitLength(left) + BitLength(right) - 1 > MaxResultBits)
			{
				throw TooBigResult();
			}

			return CheckSize(left * right);
		}

		if (right.IsZero)
		{
			throw new EvaluationException("Ділення на нуль");
		}

		// BigInteger.Divide округлює до нуля: 7/2 = 3, -7/2 = -3.
		return BigInteger.Divide(left, right);
	}

	public override BigInteger VisitAdditiveExpr(LabCalculatorParser.AdditiveExprContext context)
	{
		BigInteger left = WalkLeft(context);
		BigInteger right = WalkRight(context);
		return CheckSize(context.operatorToken.Type == LabCalculatorLexer.ADD ? left + right : left - right);
	}

	public override BigInteger VisitNumberExpr(LabCalculatorParser.NumberExprContext context)
		=> CheckSize(BigInteger.Parse(context.NUMBER().GetText(), CultureInfo.InvariantCulture));

	public override BigInteger VisitIdentifierExpr(LabCalculatorParser.IdentifierExprContext context)
		=> _cells.GetCellValue(context.IDENTIFIER().GetText().ToUpperInvariant());

	public override BigInteger VisitDeletedReferenceExpr(LabCalculatorParser.DeletedReferenceExprContext context)
		=> throw new EvaluationException("Посилання на видалену клітинку");

	private BigInteger WalkLeft(LabCalculatorParser.ExpressionContext context)
		=> Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(0));

	private BigInteger WalkRight(LabCalculatorParser.ExpressionContext context)
		=> Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(1));

	private static long BitLength(BigInteger value) => BigInteger.Abs(value).GetBitLength();

	private static BigInteger CheckSize(BigInteger value)
	{
		if (BitLength(value) > MaxResultBits)
		{
			throw TooBigResult();
		}

		return value;
	}

	private static EvaluationException TooBigResult()
		=> new($"Результат завеликий (більше {MaxResultBits} біт)");
}
