// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Builds the custom activation data a graph passes when it activates an ability. This is the inverse of
/// <see cref="AbilityActivationDataResolver"/>: instead of reading values <em>out</em> of typed data supplied by the
/// ability system, a provider builds typed data <em>from</em> the current graph state and feeds it into the activation.
/// </summary>
/// <remarks>
/// <para>Implement this once per activation-data type. The recommended way is to derive from
/// <see cref="AbilityActivationDataProvider{TData}"/>, which implements every activation entry point by calling the
/// matching generic ability API with the concrete <c>TData</c>, so the data reaches
/// <see cref="IAbilityBehavior{TData}.OnStarted"/> and <see cref="AbilityBehaviorContext{TData}.Data"/> without being
/// boxed into <see cref="object"/>.</para>
/// <para>A provider can declare authored <see cref="Inputs"/>. Each declared input renders as a nested resolver on the
/// provider's <c>Activation Data</c> section in the graph editor, and the resolved value is supplied through
/// <see cref="AbilityActivationDataInputs"/> when the data is built.</para>
/// <para>Activations whose ability behavior does not accept <see cref="DataType"/> are not an error: the ability still
/// activates through the untyped path and ignores the data.</para>
/// </remarks>
public interface IAbilityActivationDataProvider
{
	/// <summary>
	/// Gets the authored inputs this provider exposes to the graph editor. Defaults to an empty list for providers that
	/// build their data entirely from the graph state.
	/// </summary>
	IReadOnlyList<AbilityActivationDataInput> Inputs { get; }

	/// <summary>
	/// Gets the runtime type of the activation data this provider builds. Behaviors implementing
	/// <see cref="IAbilityBehavior{TData}"/> for this type receive the data through their typed
	/// <see cref="IAbilityBehavior{TData}.OnStarted"/>.
	/// </summary>
	Type DataType { get; }

	/// <summary>
	/// Builds the activation data from the current graph state and activates the ability behind
	/// <paramref name="handle"/> with it.
	/// </summary>
	/// <param name="handle">The handle of the ability to activate.</param>
	/// <param name="target">The optional activation target.</param>
	/// <param name="magnitude">The activation magnitude.</param>
	/// <param name="graphContext">The graph execution context the data is built from.</param>
	/// <param name="inputResolvers">The resolvers for the provider's declared inputs, keyed by input name.</param>
	/// <returns><see langword="true"/> when the ability was activated.</returns>
	bool Activate(
		AbilityHandle handle,
		IForgeEntity? target,
		float magnitude,
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> inputResolvers);

	/// <summary>
	/// Builds the activation data from the current graph state and activates every ability on
	/// <paramref name="abilities"/> whose ability tags overlap <paramref name="tags"/> with it.
	/// </summary>
	/// <param name="abilities">The ability manager whose abilities are activated.</param>
	/// <param name="tags">The tags selecting which abilities to activate.</param>
	/// <param name="target">The optional activation target.</param>
	/// <param name="graphContext">The graph execution context the data is built from.</param>
	/// <param name="inputResolvers">The resolvers for the provider's declared inputs, keyed by input name.</param>
	/// <returns><see langword="true"/> when at least one ability was activated.</returns>
	bool ActivateByTag(
		EntityAbilities abilities,
		TagContainer tags,
		IForgeEntity? target,
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> inputResolvers);

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
	/// <param name="inputResolvers">The resolvers for the provider's declared inputs, keyed by input name.</param>
	/// <returns><see langword="true"/> when the ability activated without failures.</returns>
	bool GrantAndActivateOnce(
		EntityAbilities abilities,
		AbilityData abilityData,
		int level,
		LevelComparison levelOverridePolicy,
		IForgeEntity? target,
		IForgeEntity? source,
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> inputResolvers);
}
