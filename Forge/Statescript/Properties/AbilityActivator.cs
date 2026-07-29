// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Providers;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Builds strongly-typed activation data through an <see cref="IAbilityActivationDataProvider"/> and activates
/// abilities with it, with no boxing. Created by <see cref="AbilityActivatorResolver"/> and used by the ability
/// activation nodes to reach the generic activation APIs.
/// </summary>
/// <param name="provider">The provider that builds the activation data.</param>
/// <param name="inputResolvers">The resolvers for the provider's declared members, keyed by input name. May be
/// <see langword="null"/> when the provider declares no members.</param>
public sealed class AbilityActivator(
	IAbilityActivationDataProvider provider,
	IReadOnlyDictionary<string, IPropertyResolver>? inputResolvers)
{
	private static readonly Dictionary<string, IPropertyResolver> _noInputs = [];

	private readonly IAbilityActivationDataProvider _provider = provider;
	private readonly IReadOnlyDictionary<string, IPropertyResolver> _inputResolvers = inputResolvers ?? _noInputs;

	/// <summary>
	/// Gets the runtime type of the activation data built by the bound provider.
	/// </summary>
	public Type DataType => _provider.DataType;

	/// <summary>
	/// Builds the activation data from the current graph state and activates the ability behind
	/// <paramref name="handle"/> with it.
	/// </summary>
	/// <param name="handle">The handle of the ability to activate.</param>
	/// <param name="target">The optional activation target.</param>
	/// <param name="magnitude">The activation magnitude.</param>
	/// <param name="graphContext">The graph execution context the data is built from.</param>
	/// <returns><see langword="true"/> when the ability was activated.</returns>
	public bool Activate(AbilityHandle handle, IForgeEntity? target, float magnitude, GraphContext graphContext)
	{
		return _provider.Activate(handle, target, magnitude, graphContext, _inputResolvers);
	}

	/// <summary>
	/// Builds the activation data from the current graph state and activates every ability on
	/// <paramref name="abilities"/> whose ability tags overlap <paramref name="tags"/> with it.
	/// </summary>
	/// <remarks>
	/// Abilities whose behavior does not accept <see cref="DataType"/> still activate; they just start through the
	/// untyped path and ignore the data.
	/// </remarks>
	/// <param name="abilities">The ability manager whose abilities are activated.</param>
	/// <param name="tags">The tags selecting which abilities to activate.</param>
	/// <param name="target">The optional activation target.</param>
	/// <param name="graphContext">The graph execution context the data is built from.</param>
	/// <returns><see langword="true"/> when at least one ability was activated.</returns>
	public bool ActivateByTag(
		EntityAbilities abilities,
		TagContainer tags,
		IForgeEntity? target,
		GraphContext graphContext)
	{
		return _provider.ActivateByTag(abilities, tags, target, graphContext, _inputResolvers);
	}

	/// <summary>
	/// Builds the activation data from the current graph state, grants <paramref name="abilityData"/> transiently on
	/// <paramref name="abilities"/>, and activates it once with the data.
	/// </summary>
	/// <param name="abilities">The ability manager receiving the transient grant.</param>
	/// <param name="abilityData">The configuration data of the ability to grant and activate.</param>
	/// <param name="level">The level at which to grant the ability.</param>
	/// <param name="levelOverridePolicy">The policy for overriding the level of an existing granted ability.</param>
	/// <param name="target">The optional activation target.</param>
	/// <param name="source">The source entity of the granted ability, if any.</param>
	/// <param name="graphContext">The graph execution context the data is built from.</param>
	/// <returns><see langword="true"/> when the ability activated without failures.</returns>
	public bool GrantAndActivateOnce(
		EntityAbilities abilities,
		AbilityData abilityData,
		int level,
		LevelComparison levelOverridePolicy,
		IForgeEntity? target,
		IForgeEntity? source,
		GraphContext graphContext)
	{
		return _provider.GrantAndActivateOnce(
			abilities,
			abilityData,
			level,
			levelOverridePolicy,
			target,
			source,
			graphContext,
			_inputResolvers);
	}
}
