// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by checking whether the ability owner entity has a specific tag. Returns
/// <see cref="bool"/> stored in a <see cref="Variant128"/>; <see langword="true"/> if the entity has the tag,
/// <see langword="false"/> otherwise.
/// </summary>
/// <remarks>
/// <para>This resolver requires the graph to be driven by an ability. It retrieves the owner entity from the
/// <see cref="AbilityBehaviorContext"/> stored in the graph's <see cref="GraphContext.ActivationContext"/>.</para>
/// <para>If the graph has no activation context or the activation context is not an
/// <see cref="AbilityBehaviorContext"/>, the resolver always returns <see langword="false"/>.</para>
/// </remarks>
/// <param name="tag">The tag to check for on the owner entity.</param>
public class TagResolver(Tag tag) : IPropertyResolver
{
	private readonly Tag _tag = tag;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return new Variant128(false);
		}

		return new Variant128(abilityContext.Owner.Tags.CombinedTags.HasTag(_tag));
	}
}
