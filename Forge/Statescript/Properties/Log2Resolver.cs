// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the base-2 logarithm of a single <see cref="IPropertyResolver"/> operand. Supports
/// <see langword="float"/> and <see langword="double"/> types. Integer operand types are promoted to
/// <see langword="double"/>. Decimal, vector, and quaternion types are not supported.
/// </summary>
/// <param name="operand">The resolver for the operand (must be positive).</param>
public class Log2Resolver(IPropertyResolver operand) : IPropertyResolver
{
	private static readonly float _ln2F = MathF.Log(2.0f);

	private static readonly double _ln2 = Math.Log(2.0);

	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointPromotedUnaryResultType(
		nameof(Log2Resolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(MathF.Log(value.AsFloat()) / _ln2F);
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				Math.Log(MathTypeUtils.ResolveAsDouble(_operand.ValueType, value)) / _ln2);
		}

		throw new InvalidOperationException(
			$"Log2Resolver encountered unexpected result type '{resultType}'.");
	}
}
