// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Base class for cue custom-parameter providers. Override <see cref="CreateCustomParameters"/> to build the parameter
/// bag from the current graph state; the base class supplies the default (empty) input list.
/// </summary>
/// <remarks>
/// Override <see cref="Inputs"/> to expose authored resolvers in the editor and read their resolved values from the
/// supplied <see cref="CueCustomParameterInputs"/>. The same keys placed in the returned dictionary are read back by
/// the cue handler.
/// </remarks>
public abstract class CueCustomParametersProvider : ICueCustomParametersProvider
{
	/// <summary>
	/// Builds the custom parameter bag for the current graph execution. Read whatever graph state the parameters are
	/// derived from (graph/shared variables, attributes, activation data, and so on) from
	/// <paramref name="graphContext"/>, and read declared <see cref="Inputs"/> from <paramref name="inputs"/>.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="inputs">The resolved values for the provider's declared <see cref="Inputs"/>.</param>
	/// <returns>The custom parameters to attach to the fired cue.</returns>
	public abstract Dictionary<StringKey, object> CreateCustomParameters(
		GraphContext graphContext,
		CueCustomParameterInputs inputs);

	/// <inheritdoc/>
	public virtual IReadOnlyList<CueCustomParameterInput> Inputs => [];
}
