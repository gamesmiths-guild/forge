// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Base class for strongly-typed effect context-data providers. Override <see cref="CreateData"/> to build a
/// <typeparamref name="TData"/> value from the current graph state; the base class wraps it in an
/// <see cref="EffectApplicationContext{TData}"/> so it flows through the effect pipeline.
/// </summary>
/// <typeparam name="TData">The type of the custom context data produced by this provider.</typeparam>
/// <remarks>
/// The same <typeparamref name="TData"/> can then be read back in a custom execution or custom modifier-magnitude
/// calculator via <see cref="EffectEvaluatedData.TryGetContextData{TData}(out TData)"/>. Override <see cref="Inputs"/>
/// to expose authored resolvers in the editor and read their resolved values from the supplied
/// <see cref="EffectContextDataInputs"/>.
/// </remarks>
public abstract class EffectContextDataProvider<TData> : IEffectContextDataProvider
{
	/// <summary>
	/// Builds the custom context data for the current graph execution. Read whatever graph state the context data is
	/// derived from (graph/shared variables, attributes, activation data, and so on) from
	/// <paramref name="graphContext"/>, and read declared <see cref="Inputs"/> from <paramref name="inputs"/>.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="inputs">The resolved values for the provider's declared <see cref="Inputs"/>.</param>
	/// <returns>The custom context data to pass to the effect application.</returns>
	public abstract TData CreateData(GraphContext graphContext, EffectContextDataInputs inputs);

	/// <inheritdoc/>
	public virtual IReadOnlyList<EffectContextDataInput> Inputs => [];

	/// <inheritdoc/>
	public EffectApplicationContext CreateContext(GraphContext graphContext, EffectContextDataInputs inputs)
	{
		return new EffectApplicationContext<TData>(CreateData(graphContext, inputs));
	}
}
