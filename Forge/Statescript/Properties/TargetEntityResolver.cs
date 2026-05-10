// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the target entity from the current <see cref="AbilityBehaviorContext"/>.
/// </summary>
public class TargetEntityResolver : ReferenceResolver<IForgeEntity>, IEntityResolver
{
	/// <inheritdoc/>
	public override IForgeEntity? Resolve(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return null;
		}

		return abilityContext.Target;
	}
}
