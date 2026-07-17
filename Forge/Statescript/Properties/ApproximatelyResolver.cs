// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see langword="bool"/> indicating whether two numeric values are approximately equal within a
/// tolerance.
/// </summary>
/// <remarks>
/// Use this instead of an equality comparison for floating-point values, where exact equality is a footgun.
/// </remarks>
/// <param name="a">The resolver for the first value.</param>
/// <param name="b">The resolver for the second value.</param>
/// <param name="tolerance">The maximum absolute difference considered equal.</param>
public class ApproximatelyResolver(IPropertyResolver a, IPropertyResolver b, double tolerance = 1e-6)
	: IPropertyResolver
{
	private readonly IPropertyResolver _a = a;

	private readonly IPropertyResolver _b = b;

	private readonly double _tolerance = tolerance >= 0
		? tolerance
		: throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance cannot be negative.");

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		double doubleA = MathTypeUtils.ResolveAsDouble(_a.ValueType, _a.Resolve(graphContext));
		double doubleB = MathTypeUtils.ResolveAsDouble(_b.ValueType, _b.Resolve(graphContext));

		return new Variant128(Math.Abs(doubleA - doubleB) <= _tolerance);
	}
}
