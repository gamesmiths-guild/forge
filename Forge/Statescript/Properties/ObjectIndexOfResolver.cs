// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the zero-based index of the first occurrence of a given reference in a nested object-backed array resolver,
/// or -1 when the reference is not present. Elements are matched by reference identity.
/// </summary>
/// <remarks>
/// A <see langword="null"/> search value matches stored <see langword="null"/> elements.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="value">The resolver providing the reference to search for.</param>
public class ObjectIndexOfResolver(IObjectArrayResolver source, IObjectResolver value) : IPropertyResolver
{
	private readonly IObjectArrayResolver _source = source;

	private readonly IObjectResolver _value = value;

	/// <inheritdoc/>
	public Type ValueType => typeof(int);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		object?[] values = _source.ResolveArray(graphContext);
		object? target = _value.Resolve(graphContext);

		for (int i = 0; i < values.Length; i++)
		{
			if (ReferenceEquals(values[i], target))
			{
				return new Variant128(i);
			}
		}

		return new Variant128(-1);
	}
}
