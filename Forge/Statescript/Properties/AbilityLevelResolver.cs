// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the current ability level from the active <see cref="AbilityBehaviorContext"/>.
/// </summary>
public class AbilityLevelResolver : IPropertyResolver
{
	/// <inheritdoc/>
	public Type ValueType => typeof(int);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return default;
		}

		return new Variant128(abilityContext.Level);
	}
}
