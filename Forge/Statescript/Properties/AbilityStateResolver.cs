// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a state flag (<see langword="bool"/>) from an ability.
/// </summary>
/// <remarks>
/// <para>By default the resolver reads the ability driving the current graph through the active
/// <see cref="AbilityBehaviorContext"/>. Provide a handle resolver to inspect a different ability.</para>
/// <para>Missing abilities resolve to <see langword="false"/>.</para>
/// </remarks>
/// <param name="stateType">Which state flag to read.</param>
/// <param name="handleResolver">Optional resolver producing the ability handle to inspect.</param>
public class AbilityStateResolver(
	AbilityStateType stateType,
	IObjectResolver<AbilityHandle>? handleResolver = null) : IPropertyResolver
{
	private readonly AbilityStateType _stateType = stateType;
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

		return _stateType switch
		{
			AbilityStateType.IsActive => new Variant128(handle.IsActive),
			AbilityStateType.IsInhibited => new Variant128(handle.IsInhibited),
			AbilityStateType.IsValid => new Variant128(handle.IsValid),
			_ => default,
		};
	}
}
