// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the current effect ownership from the active <see cref="AbilityBehaviorContext"/>.
/// </summary>
public class AbilityOwnershipResolver : ObjectResolver<EffectOwnership>
{
	/// <inheritdoc/>
	public override EffectOwnership Resolve(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return new EffectOwnership(null, null);
		}

		return abilityContext.Ownership;
	}
}
