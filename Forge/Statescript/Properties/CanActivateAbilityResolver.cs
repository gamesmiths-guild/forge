// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see langword="bool"/> indicating whether an ability can currently activate.
/// </summary>
/// <remarks>
/// <para>By default the resolver reads the ability driving the current graph through the active
/// <see cref="AbilityBehaviorContext"/>. Provide a handle resolver to inspect a different ability.</para>
/// <para>The check covers cooldowns, costs, tag requirements and blocking, exactly like activating the ability
/// would. Missing abilities resolve to <see langword="false"/>.</para>
/// </remarks>
/// <param name="targetResolver">Optional resolver producing the activation target used by target tag requirement
/// checks.</param>
/// <param name="handleResolver">Optional resolver producing the ability handle to inspect.</param>
public class CanActivateAbilityResolver(
	IEntityResolver? targetResolver = null,
	IObjectResolver<AbilityHandle>? handleResolver = null) : IPropertyResolver
{
	private readonly IEntityResolver? _targetResolver = targetResolver;
	private readonly IObjectResolver<AbilityHandle>? _handleResolver = handleResolver;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		AbilityHandle? handle = AbilityResolverUtilities.ResolveHandle(graphContext, _handleResolver);

		if (handle is null)
		{
			return new Variant128(false);
		}

		IForgeEntity? target = _targetResolver?.Resolve(graphContext);

		return new Variant128(handle.CanActivate(out _, target));
	}
}
