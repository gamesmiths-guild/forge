// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript.Providers;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Object resolver that produces the <see cref="EffectApplicationContext"/> for an effect application by delegating to
/// an <see cref="IEffectContextDataProvider"/>.
/// </summary>
/// <remarks>
/// Bound to the optional context-data input of <c>ApplyEffectNode</c>/<c>EffectNode</c>. The resolved context flows
/// through the effect pipeline when the node applies the effect, so it can be read with
/// <see cref="EffectEvaluatedData.TryGetContextData{TData}(out TData)"/>. When the provider declares authored inputs,
/// the matching <paramref name="inputResolvers"/> resolve them on demand.
/// </remarks>
/// <param name="provider">The provider that builds the application context from the graph state.</param>
/// <param name="inputResolvers">The resolvers for the provider's declared inputs, keyed by input name. May be
/// <see langword="null"/> when the provider declares no inputs.</param>
public class EffectContextDataResolver(
	IEffectContextDataProvider provider,
	IReadOnlyDictionary<string, IPropertyResolver>? inputResolvers = null) : ObjectResolver<EffectApplicationContext>
{
	private static readonly Dictionary<string, IPropertyResolver> _noInputs = [];

	private readonly IEffectContextDataProvider _provider = provider;
	private readonly IReadOnlyDictionary<string, IPropertyResolver> _inputResolvers = inputResolvers ?? _noInputs;

	/// <inheritdoc/>
	public override EffectApplicationContext Resolve(GraphContext graphContext)
	{
		var inputs = new EffectContextDataInputs(graphContext, _inputResolvers);
		return _provider.CreateContext(graphContext, inputs);
	}
}
