// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the concatenation of two nested array resolvers, producing the elements of the first followed by the
/// elements of the second.
/// </summary>
/// <param name="first">The resolver providing the leading elements.</param>
/// <param name="second">The resolver providing the trailing elements. Must share the first resolver's element type.
/// </param>
public class ConcatResolver(IArrayPropertyResolver first, IArrayPropertyResolver second) : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _first = first;

	private readonly IArrayPropertyResolver _second = second;

	/// <inheritdoc/>
	public Type ElementType { get; } = ArrayResolverUtils.ValidateMatchingElementTypes(
		nameof(ConcatResolver),
		first.ElementType,
		second.ElementType);

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] firstValues = _first.ResolveArray(graphContext);
		Variant128[] secondValues = _second.ResolveArray(graphContext);

		if (firstValues.Length == 0)
		{
			return secondValues;
		}

		if (secondValues.Length == 0)
		{
			return firstValues;
		}

		var result = new Variant128[firstValues.Length + secondValues.Length];
		Array.Copy(firstValues, result, firstValues.Length);
		Array.Copy(secondValues, 0, result, firstValues.Length, secondValues.Length);
		return result;
	}
}
