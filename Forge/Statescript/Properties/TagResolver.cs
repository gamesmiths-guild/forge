// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by checking whether the graph owner entity has a specific tag. Returns
/// <see cref="bool"/> stored in a <see cref="Variant128"/>; <see langword="true"/> if the entity has
/// the tag, <see langword="false"/> otherwise.
/// </summary>
/// <remarks>
/// If the graph context has no owner, the resolver always returns <see langword="false"/>.
/// </remarks>
/// <param name="tag">The tag to check for on the owner entity.</param>
public class TagResolver(Tag tag) : IPropertyResolver
{
	private readonly Tag _tag = tag;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(IGraphContext graphContext)
	{
		if (graphContext.Owner is null)
		{
			return new Variant128(false);
		}

		return new Variant128(graphContext.Owner.Tags.CombinedTags.HasTag(_tag));
	}
}
