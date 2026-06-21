// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Builds the custom parameter bag (<see cref="Cues.CueParameters.CustomParameters"/>) passed when a graph fires a cue.
/// A provider reads whatever graph state it needs and returns a dictionary keyed by <see cref="StringKey"/>, which the
/// cue handler later reads back by key.
/// </summary>
/// <remarks>
/// <para>The recommended way is to derive from <see cref="CueCustomParametersProvider"/>, which supplies the default
/// (empty) input list so implementations only override <see cref="CreateCustomParameters"/>.</para>
/// <para>A provider can declare authored <see cref="Inputs"/>. Each declared input renders as a nested resolver on the
/// provider's <c>Custom Parameters</c> section in the graph editor, and the resolved value is supplied through
/// <see cref="CueCustomParameterInputs"/> when the bag is built.</para>
/// </remarks>
public interface ICueCustomParametersProvider
{
	/// <summary>
	/// Gets the authored inputs this provider exposes to the graph editor. Defaults to an empty list for providers that
	/// build their parameters entirely from the graph state.
	/// </summary>
	IReadOnlyList<CueCustomParameterInput> Inputs { get; }

	/// <summary>
	/// Builds the custom parameter bag for the current graph execution.
	/// </summary>
	/// <param name="graphContext">The graph execution context, used to read whatever graph state the parameters are
	/// derived from (graph/shared variables, attributes, activation data, and so on).</param>
	/// <param name="inputs">The resolved values for the provider's declared <see cref="Inputs"/>.</param>
	/// <returns>The custom parameters to attach to the fired cue.</returns>
	Dictionary<StringKey, object> CreateCustomParameters(GraphContext graphContext, CueCustomParameterInputs inputs);
}
