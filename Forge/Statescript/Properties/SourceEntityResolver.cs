// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the source entity from the current <see cref="AbilityBehaviorContext"/>.
/// </summary>
public class SourceEntityResolver : ReferenceResolver<IForgeEntity>, IEntityResolver
{
	/// <inheritdoc/>
	public override IForgeEntity? Resolve(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return null;
		}

		return abilityContext.Source;
	}
}
