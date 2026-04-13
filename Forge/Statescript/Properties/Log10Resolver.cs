// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the base-10 logarithm of a single <see cref="IPropertyResolver"/> operand. Supports
/// <see langword="float"/> and <see langword="double"/> types. Integer operand types are promoted to
/// <see langword="double"/>. Decimal, vector, and quaternion types are not supported.
/// </summary>
/// <param name="operand">The resolver for the operand (must be positive).</param>
public class Log10Resolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointPromotedUnaryResultType(
		nameof(Log10Resolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(MathF.Log10(value.AsFloat()));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(Math.Log10(MathTypeUtils.ResolveAsDouble(_operand.ValueType, value)));
		}

		throw new InvalidOperationException(
			$"Log10Resolver encountered unexpected result type '{resultType}'.");
	}
}
