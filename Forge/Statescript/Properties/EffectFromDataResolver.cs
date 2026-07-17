// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Builds an <see cref="Effect"/> instance from an <see cref="EffectData"/> value plus optional level, ownership and
/// SetByCaller magnitude resolvers.
/// </summary>
/// <remarks>
/// <para>When no level resolver is provided, the level falls back to the active <see cref="AbilityBehaviorContext"/>
/// level, or <c>1</c> when there is no ability context. When no ownership resolver is provided, the ownership falls
/// back to the active ability ownership, or an empty ownership when there is no ability context.</para>
/// <para>SetByCaller magnitudes are resolved and written to the freshly built effect on every resolve, guaranteeing
/// that effects with <see cref="SetByCallerFloat"/> magnitudes have their values set before application.</para>
/// </remarks>
/// <param name="effectData">The effect configuration data used to build the effect.</param>
/// <param name="levelResolver">Optional resolver used for the effect level.</param>
/// <param name="ownershipResolver">Optional resolver used for the effect ownership.</param>
/// <param name="setByCallerMagnitudes">Optional SetByCaller magnitude resolvers, keyed by identifier tag, applied to
/// the built effect.</param>
public class EffectFromDataResolver(
	EffectData effectData,
	IPropertyResolver? levelResolver = null,
	IObjectResolver<EffectOwnership>? ownershipResolver = null,
	IReadOnlyList<KeyValuePair<Tag, IPropertyResolver>>? setByCallerMagnitudes = null) : ObjectResolver<Effect>
{
	private readonly EffectData _effectData = effectData;
	private readonly IPropertyResolver? _levelResolver = levelResolver;
	private readonly IObjectResolver<EffectOwnership>? _ownershipResolver = ownershipResolver;

	private readonly IReadOnlyList<KeyValuePair<Tag, IPropertyResolver>>? _setByCallerMagnitudes =
		setByCallerMagnitudes;

	/// <inheritdoc/>
	public override Effect Resolve(GraphContext graphContext)
	{
		int level = EffectResolverUtilities.ResolveLevel(graphContext, _levelResolver);
		EffectOwnership ownership = EffectResolverUtilities.ResolveOwnership(graphContext, _ownershipResolver);

		var effect = new Effect(_effectData, ownership, level);

		EffectResolverUtilities.ApplySetByCallerMagnitudes(graphContext, effect, _setByCallerMagnitudes);

		return effect;
	}
}
