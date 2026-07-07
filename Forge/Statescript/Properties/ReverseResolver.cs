// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the elements of a nested array resolver in reverse order.
/// </summary>
/// <param name="source">The resolver providing the source array.</param>
public class ReverseResolver(IArrayPropertyResolver source) : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	/// <inheritdoc/>
	public Type ElementType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);

		if (values.Length <= 1)
		{
			return values;
		}

		var result = new Variant128[values.Length];
		Array.Copy(values, result, values.Length);
		Array.Reverse(result);
		return result;
	}
}
