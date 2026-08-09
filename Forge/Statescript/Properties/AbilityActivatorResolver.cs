// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Providers;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Object resolver that produces an <see cref="AbilityActivator"/> for the optional activation-data input of
/// <c>TryActivateAbilityNode</c>, <c>TryActivateAbilitiesByTagNode</c>, and
/// <c>TryGrantAbilityAndActivateOnceNode</c>. The node uses the activator to build the bound
/// <see cref="IAbilityActivationDataProvider"/>'s typed data and pass it through the generic (non-boxing) activation
/// APIs.
/// </summary>
/// <remarks>
/// When the provider declares members, the matching <paramref name="inputResolvers"/> resolve them on demand as the
/// data is built.
/// </remarks>
/// <param name="provider">The provider that builds the activation data from the graph state.</param>
/// <param name="inputResolvers">The resolvers for the provider's declared members, keyed by member name. May be
/// <see langword="null"/> when the provider declares no members.</param>
public class AbilityActivatorResolver(
	IAbilityActivationDataProvider provider,
	IReadOnlyDictionary<string, IPropertyResolver>? inputResolvers = null) : ObjectResolver<AbilityActivator>
{
	private readonly AbilityActivator _activator = new(provider, inputResolvers);

	/// <inheritdoc/>
	public override AbilityActivator Resolve(GraphContext graphContext)
	{
		return _activator;
	}
}
