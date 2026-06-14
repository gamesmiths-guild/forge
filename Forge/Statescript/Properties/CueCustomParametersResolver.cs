// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Object resolver that produces the custom parameter bag for a cue by delegating to an
/// <see cref="ICueCustomParametersProvider"/>.
/// </summary>
/// <remarks>
/// Bound to the optional custom-parameters input of the cue nodes. The resolved dictionary is attached to the cue's
/// <see cref="Cues.CueParameters.CustomParameters"/> when the node fires the cue, so a cue handler can read it back by
/// key. When the provider declares authored inputs, the matching <paramref name="inputResolvers"/> resolve them on
/// demand.
/// </remarks>
/// <param name="provider">The provider that builds the custom parameter bag from the graph state.</param>
/// <param name="inputResolvers">The resolvers for the provider's declared inputs, keyed by input name. May be
/// <see langword="null"/> when the provider declares no inputs.</param>
public class CueCustomParametersResolver(
	ICueCustomParametersProvider provider,
	IReadOnlyDictionary<string, IPropertyResolver>? inputResolvers = null)
	: ObjectResolver<Dictionary<StringKey, object>>
{
	private static readonly Dictionary<string, IPropertyResolver> _noInputs = [];

	private readonly ICueCustomParametersProvider _provider = provider;
	private readonly IReadOnlyDictionary<string, IPropertyResolver> _inputResolvers = inputResolvers ?? _noInputs;

	/// <inheritdoc/>
	public override Dictionary<StringKey, object> Resolve(GraphContext graphContext)
	{
		var inputs = new CueCustomParameterInputs(graphContext, _inputResolvers);
		return _provider.CreateCustomParameters(graphContext, inputs);
	}
}
