// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Builds an <see cref="Effect"/> instance from an <see cref="EffectData"/> value plus optional level and ownership
/// resolvers.
/// </summary>
/// <remarks>
/// When no level resolver is provided, the level falls back to the active <see cref="AbilityBehaviorContext"/> level,
/// or <c>1</c> when there is no ability context. When no ownership resolver is provided, the ownership falls back to
/// the active ability ownership, or an empty ownership when there is no ability context.
/// </remarks>
/// <param name="effectData">The effect configuration data used to build the effect.</param>
/// <param name="levelResolver">Optional resolver used for the effect level.</param>
/// <param name="ownershipResolver">Optional resolver used for the effect ownership.</param>
public class EffectFromDataResolver(
	EffectData effectData,
	IPropertyResolver? levelResolver = null,
	IObjectResolver<EffectOwnership>? ownershipResolver = null) : ObjectResolver<Effect>
{
	private readonly EffectData _effectData = effectData;
	private readonly IPropertyResolver? _levelResolver = levelResolver;
	private readonly IObjectResolver<EffectOwnership>? _ownershipResolver = ownershipResolver;

	/// <inheritdoc/>
	public override Effect Resolve(GraphContext graphContext)
	{
		int level = EffectResolverUtilities.ResolveLevel(graphContext, _levelResolver);
		EffectOwnership ownership = EffectResolverUtilities.ResolveOwnership(graphContext, _ownershipResolver);

		return new Effect(_effectData, ownership, level);
	}
}
