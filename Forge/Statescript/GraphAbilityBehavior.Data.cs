// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// An <see cref="IAbilityBehavior{TData}"/> implementation that drives an ability's lifecycle through a
/// <see cref="Graph"/>, with support for strongly-typed activation data. The <paramref name="dataBinder"/> maps
/// <typeparamref name="TData"/> fields into graph variables before the graph begins processing, allowing nodes to
/// consume activation data through the standard property/variable system.
/// </summary>
/// <typeparam name="TData">The type of the activation data expected from the ability system.</typeparam>
/// <param name="graph">The graph definition to execute when the ability activates.</param>
/// <param name="graphContext">The per-instance context that holds mutable runtime state for the graph.</param>
/// <param name="dataBinder">A delegate that writes <typeparamref name="TData"/> fields into graph
/// <see cref="Variables"/> before the graph starts. Use <see cref="Variables.SetVar{T}"/> to map each relevant field to
/// the graph variable expected by the graph's nodes.</param>
public class GraphAbilityBehavior<TData>(Graph graph, GraphContext graphContext, Action<TData, Variables> dataBinder)
	: GraphAbilityBehavior(graph, graphContext), IAbilityBehavior<TData>
{
	private readonly Action<TData, Variables> _dataBinder = dataBinder;

	/// <inheritdoc/>
	public void OnStarted(AbilityBehaviorContext context, TData data)
	{
		StartGraph(context, x => _dataBinder(data, x));
	}
}
