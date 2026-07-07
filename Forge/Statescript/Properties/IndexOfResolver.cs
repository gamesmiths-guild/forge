// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the zero-based index of the first occurrence of a given value in a nested array resolver, or -1 when the
/// value is not present. The value is itself a nested resolver, allowing both constants and computed values.
/// </summary>
/// <remarks>
/// Floating-point elements are compared exactly.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="value">The resolver providing the value to search for. Must resolve to the source element type.
/// </param>
public class IndexOfResolver(IArrayPropertyResolver source, IPropertyResolver value) : IPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	private readonly IPropertyResolver _value =
		ArrayResolverUtils.ValidateElementOperand(nameof(IndexOfResolver), nameof(value), source.ElementType, value);

	private readonly Type _elementType = source.ElementType;

	/// <inheritdoc/>
	public Type ValueType => typeof(int);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);
		Variant128 target = _value.Resolve(graphContext);

		for (int i = 0; i < values.Length; i++)
		{
			if (VariantEquality.AreEqual(values[i], target, _elementType))
			{
				return new Variant128(i);
			}
		}

		return new Variant128(-1);
	}
}
