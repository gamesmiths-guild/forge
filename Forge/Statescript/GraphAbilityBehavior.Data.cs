// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// An <see cref="IAbilityBehavior{TData}"/> implementation that drives an ability's lifecycle through a
/// <see cref="Graph"/>, with support for strongly-typed activation data. The optional <paramref name="dataBinder"/>
/// maps <typeparamref name="TData"/> fields into graph variables before the graph begins processing, while
/// <see cref="Properties.ActivationDataResolver"/> can read fields directly from the activation context when graph
/// variables are not needed.
/// </summary>
/// <typeparam name="TData">The type of the activation data expected from the ability system.</typeparam>
/// <param name="graph">The graph definition to execute when the ability activates.</param>
/// <param name="dataBinder">A delegate that writes <typeparamref name="TData"/> fields into graph
/// <see cref="Variables"/> before the graph starts. Use <see cref="Variables.SetVar{T}"/> to map each relevant field to
/// the graph variable expected by the graph's nodes.</param>
public class GraphAbilityBehavior<TData>(Graph graph, Action<TData, Variables> dataBinder)
	: GraphAbilityBehavior(graph), IAbilityBehavior<TData>
{
	private readonly Action<TData, Variables> _dataBinder = dataBinder;

	/// <summary>
	/// Initializes a new instance of the <see cref="GraphAbilityBehavior{TData}"/> class without a variable binder.
	/// Use this when the graph reads activation data directly from <see cref="GraphContext.ActivationContext"/>, for
	/// example through <see cref="Properties.ActivationDataResolver"/>.
	/// </summary>
	/// <param name="graph">The graph definition to execute when the ability activates.</param>
	public GraphAbilityBehavior(Graph graph)
		: this(graph, static (_, _) => { })
	{
	}

	/// <inheritdoc/>
	public void OnStarted(AbilityBehaviorContext context, TData data)
	{
		StartGraph(context, x => _dataBinder(data, x));
	}
}
