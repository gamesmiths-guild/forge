// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see cref="bool"/> indicating whether a nested object-backed array resolver contains a given reference,
/// using reference identity (e.g. "is this entity already in the target list?").
/// </summary>
/// <remarks>
/// A <see langword="null"/> search value matches stored <see langword="null"/> elements. Combine with
/// <see cref="IsValidResolver"/> when missing values must not count as a match.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="value">The resolver providing the reference to search for.</param>
public class ObjectContainsResolver(IObjectArrayResolver source, IObjectResolver value) : IPropertyResolver
{
	private readonly IObjectArrayResolver _source = source;

	private readonly IObjectResolver _value = value;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		object?[] values = _source.ResolveArray(graphContext);
		object? target = _value.Resolve(graphContext);

		for (int i = 0; i < values.Length; i++)
		{
			if (ReferenceEquals(values[i], target))
			{
				return new Variant128(true);
			}
		}

		return new Variant128(false);
	}
}
