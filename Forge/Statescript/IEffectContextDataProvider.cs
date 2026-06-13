// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Produces an <see cref="EffectApplicationContext"/> to pass as custom context data when a graph applies an effect.
/// This is the inverse of <see cref="Properties.AbilityActivationDataResolver"/>: instead of reading values
/// <em>out</em> of typed data supplied by the ability system, a provider builds typed data <em>from</em> the current
/// graph state and feeds it into the effect pipeline.
/// </summary>
/// <remarks>
/// <para>Implement this once per context-data type. The recommended way is to derive from
/// <see cref="EffectContextDataProvider{TData}"/>, which seals the boxing into an
/// <see cref="EffectApplicationContext{TData}"/> so the data can later be read with
/// <see cref="EffectEvaluatedData.TryGetContextData{TData}(out TData)"/> in a custom calculator or execution.</para>
/// <para>A provider can declare authored <see cref="Inputs"/>. Each declared input renders as a nested resolver on the
/// provider's <c>Context Data</c> section in the graph editor, and the resolved value is supplied through
/// <see cref="EffectContextDataInputs"/> when the context is built.</para>
/// </remarks>
public interface IEffectContextDataProvider
{
	/// <summary>
	/// Gets the authored inputs this provider exposes to the graph editor. Defaults to an empty list for providers that
	/// build their data entirely from the graph state.
	/// </summary>
	IReadOnlyList<EffectContextDataInput> Inputs { get; }

	/// <summary>
	/// Builds the application context for the current graph execution.
	/// </summary>
	/// <param name="graphContext">The graph execution context, used to read whatever graph state the context data is
	/// derived from (graph/shared variables, attributes, activation data, and so on).</param>
	/// <param name="inputs">The resolved values for the provider's declared <see cref="Inputs"/>.</param>
	/// <returns>The application context to pass to the effect application.</returns>
	EffectApplicationContext CreateContext(GraphContext graphContext, EffectContextDataInputs inputs);
}
