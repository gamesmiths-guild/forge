// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Builds an array of <see cref="Effect"/> instances from an array of <see cref="EffectData"/> values plus optional
/// level, ownership and SetByCaller magnitude resolvers, applying the same level, ownership and SetByCaller values to
/// every produced effect.
/// </summary>
/// <remarks>
/// Level, ownership and SetByCaller magnitudes fall back to or follow the same rules as
/// <see cref="EffectFromDataResolver"/>.
/// </remarks>
/// <param name="effectData">The effect configuration data values used to build the effects.</param>
/// <param name="levelResolver">Optional resolver used for the effect level.</param>
/// <param name="ownershipResolver">Optional resolver used for the effect ownership.</param>
/// <param name="setByCallerMagnitudes">Optional SetByCaller magnitude resolvers, keyed by identifier tag, applied to
/// every built effect.</param>
public class EffectArrayFromDataResolver(
	EffectData[] effectData,
	IPropertyResolver? levelResolver = null,
	IObjectResolver<EffectOwnership>? ownershipResolver = null,
	IReadOnlyList<KeyValuePair<Tag, IPropertyResolver>>? setByCallerMagnitudes = null) : ObjectArrayResolver<Effect>
{
	private readonly EffectData[] _effectData = effectData;
	private readonly IPropertyResolver? _levelResolver = levelResolver;
	private readonly IObjectResolver<EffectOwnership>? _ownershipResolver = ownershipResolver;

	private readonly IReadOnlyList<KeyValuePair<Tag, IPropertyResolver>>? _setByCallerMagnitudes =
		setByCallerMagnitudes;

	/// <inheritdoc/>
	public override Effect[] ResolveArray(GraphContext graphContext)
	{
		int level = EffectResolverUtilities.ResolveLevel(graphContext, _levelResolver);
		EffectOwnership ownership = EffectResolverUtilities.ResolveOwnership(graphContext, _ownershipResolver);

		var effects = new Effect[_effectData.Length];

		for (int i = 0; i < _effectData.Length; i++)
		{
			effects[i] = new Effect(_effectData[i], ownership, level);

			EffectResolverUtilities.ApplySetByCallerMagnitudes(graphContext, effects[i], _setByCallerMagnitudes);
		}

		return effects;
	}
}
