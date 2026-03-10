// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Property resolver that retrieves the magnitude of the current ability behavior context. This is useful for nodes
/// that need to access the magnitude of an ability's effect.
/// </summary>
public class MagnitudeResolver : IPropertyResolver
{
	/// <inheritdoc/>
	public Type ValueType => typeof(float);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? context))
		{
			return default;
		}

		return new Variant128(context.Magnitude);
	}
}
