// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Base class for strongly-typed ability activation-data providers. Override <see cref="CreateData"/> to build a
/// <typeparamref name="TData"/> value from the current graph state; the base class calls the generic ability APIs with
/// it, so the data arrives at <see cref="IAbilityBehavior{TData}.OnStarted"/> and
/// <see cref="AbilityBehaviorContext{TData}.Data"/> with its concrete type intact.
/// </summary>
/// <typeparam name="TData">The type of the activation data produced by this provider.</typeparam>
/// <remarks>
/// <para>The same <typeparamref name="TData"/> is then read back inside the activated ability's graph through
/// <see cref="AbilityActivationDataResolver"/>, or by a <see cref="GraphAbilityBehavior{TData}"/> data binder.</para>
/// <para>Override <see cref="Members"/> to declare the data's members once. They become authored resolvers on the
/// sending node, read from the supplied <see cref="AbilityActivationDataInputs"/>, and bindable fields on the reading
/// node. Member names must match the public fields or properties of <typeparamref name="TData"/>, since the reading
/// side resolves them there by name.</para>
/// </remarks>
public abstract class AbilityActivationDataProvider<TData> : IAbilityActivationDataProvider
{
	/// <summary>
	/// Builds the activation data for the current graph execution. Read whatever graph state the data is derived from
	/// (graph/shared variables, attributes, activation data, and so on) from <paramref name="graphContext"/>, and read
	/// declared <see cref="Members"/> from <paramref name="inputs"/>.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="inputs">The resolved values for the provider's declared <see cref="Members"/>.</param>
	/// <returns>The activation data to pass to the ability activation.</returns>
	public abstract TData CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs);

	/// <inheritdoc/>
	public virtual IReadOnlyList<AbilityActivationDataMember> Members => [];

	/// <inheritdoc/>
	public Type DataType => typeof(TData);

	/// <inheritdoc/>
	bool IAbilityActivationDataProvider.Activate(
		AbilityHandle handle,
		IForgeEntity? target,
		float magnitude,
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> inputResolvers)
	{
		return handle.TryActivate(BuildData(graphContext, inputResolvers), out _, target, magnitude);
	}

	/// <inheritdoc/>
	bool IAbilityActivationDataProvider.ActivateByTag(
		EntityAbilities abilities,
		TagContainer tags,
		IForgeEntity? target,
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> inputResolvers)
	{
		return abilities.TryActivateAbilitiesByTag(tags, target, BuildData(graphContext, inputResolvers), out _);
	}

	/// <inheritdoc/>
	bool IAbilityActivationDataProvider.GrantAndActivateOnce(
		EntityAbilities abilities,
		AbilityData abilityData,
		int level,
		LevelComparison levelOverridePolicy,
		IForgeEntity? target,
		IForgeEntity? source,
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> inputResolvers,
		out AbilityHandle? grantedAbility)
	{
		return abilities.TryGrantAbilityAndActivateOnce(
			abilityData,
			level,
			levelOverridePolicy,
			BuildData(graphContext, inputResolvers),
			out _,
			out grantedAbility,
			target,
			source);
	}

	private TData BuildData(
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> inputResolvers)
	{
		return CreateData(graphContext, new AbilityActivationDataInputs(graphContext, inputResolvers));
	}
}
