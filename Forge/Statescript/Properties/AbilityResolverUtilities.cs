// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Shared ability-handle resolution helpers used by the ability data resolvers.
/// </summary>
internal static class AbilityResolverUtilities
{
	public static AbilityHandle? ResolveHandle(
		GraphContext graphContext,
		IObjectResolver<AbilityHandle>? handleResolver)
	{
		if (handleResolver is not null)
		{
			return handleResolver.Resolve(graphContext);
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.AbilityHandle
			: null;
	}
}
