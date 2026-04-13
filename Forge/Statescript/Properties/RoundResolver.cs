// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves rounding of a single <see cref="IPropertyResolver"/> operand. Supports <see langword="float"/>,
/// <see langword="double"/>, and <see langword="decimal"/> types only. Integer, vector, and quaternion types are not
/// supported.
/// </summary>
/// <param name="operand">The resolver for the operand.</param>
/// <param name="digits">The number of fractional digits in the return value. Defaults to <c>0</c> (round to nearest
/// integer).</param>
/// <param name="mode">The rounding strategy to use. Defaults to <see cref="MidpointRounding.ToEven"/> (banker's
/// rounding).</param>
public class RoundResolver(
	IPropertyResolver operand,
	int digits = 0,
	MidpointRounding mode = MidpointRounding.ToEven) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	private readonly int _digits = ValidateDigits(digits);

	private readonly MidpointRounding _mode = mode;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointUnaryResultType(
		nameof(RoundResolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(MathF.Round(value.AsFloat(), _digits, _mode));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(Math.Round(value.AsDouble(), _digits, _mode));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(Math.Round(value.AsDecimal(), _digits, _mode));
		}

		throw new InvalidOperationException($"RoundResolver encountered unexpected result type '{resultType}'.");
	}

	private static int ValidateDigits(int digits)
	{
		if (digits < 0 || digits > 15)
		{
			throw new ArgumentOutOfRangeException(
				nameof(digits),
				digits,
				"Digits must be between 0 and 15.");
		}

		return digits;
	}
}
