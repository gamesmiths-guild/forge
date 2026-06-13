// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Builds an array of <see cref="Effect"/> instances from an array of <see cref="EffectData"/> values plus optional
/// level and ownership resolvers, applying the same level and ownership to every produced effect.
/// </summary>
/// <remarks>
/// Level and ownership fall back to the active ability context using the same rules as
/// <see cref="EffectFromDataResolver"/>.
/// </remarks>
/// <param name="effectData">The effect configuration data values used to build the effects.</param>
/// <param name="levelResolver">Optional resolver used for the effect level.</param>
/// <param name="ownershipResolver">Optional resolver used for the effect ownership.</param>
public class EffectArrayFromDataResolver(
	EffectData[] effectData,
	IPropertyResolver? levelResolver = null,
	IObjectResolver<EffectOwnership>? ownershipResolver = null) : ObjectArrayResolver<Effect>
{
	private readonly EffectData[] _effectData = effectData;
	private readonly IPropertyResolver? _levelResolver = levelResolver;
	private readonly IObjectResolver<EffectOwnership>? _ownershipResolver = ownershipResolver;

	/// <inheritdoc/>
	public override Effect[] ResolveArray(GraphContext graphContext)
	{
		int level = EffectResolverUtilities.ResolveLevel(graphContext, _levelResolver);
		EffectOwnership ownership = EffectResolverUtilities.ResolveOwnership(graphContext, _ownershipResolver);

		var effects = new Effect[_effectData.Length];

		for (int i = 0; i < _effectData.Length; i++)
		{
			effects[i] = new Effect(_effectData[i], ownership, level);
		}

		return effects;
	}
}
