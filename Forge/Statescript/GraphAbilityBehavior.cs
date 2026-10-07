// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// An <see cref="IAbilityBehavior"/> implementation that drives an ability's lifecycle through a <see cref="Graph"/>.
/// When the ability starts, the graph begins processing; when the graph completes naturally (all state nodes
/// deactivate) or reaches an <see cref="Nodes.ExitNode"/>, the ability instance ends automatically. Canceling the
/// ability stops the graph immediately.
/// </summary>
/// <remarks>
/// <para>Because <see cref="IAbilityBehavior"/> has no update method, the caller must tick the <see cref="Processor"/>
/// each frame via <see cref="GraphProcessor.UpdateGraph"/>. A typical pattern is to call it from the entity's game loop
/// alongside <see cref="Effects.EffectsManager.UpdateEffects(double)"/>.</para>
/// </remarks>
/// <param name="graph">The graph definition to execute when the ability activates.</param>
public class GraphAbilityBehavior(Graph graph) : IAbilityBehavior
{
	/// <summary>
	/// Gets the <see cref="GraphProcessor"/> that drives this behavior. Callers must invoke
	/// <see cref="GraphProcessor.UpdateGraph"/> each frame to advance time-dependent state nodes.
	/// </summary>
	public GraphProcessor Processor { get; } = new GraphProcessor(graph);

	/// <inheritdoc/>
	public void OnStarted(AbilityBehaviorContext context)
	{
		StartGraph(context);
	}

	/// <inheritdoc/>
	public void OnEnded(AbilityBehaviorContext context)
	{
		StopGraph();
	}

	/// <inheritdoc/>
	public void OnUpdate(double deltaTime)
	{
		Processor.UpdateGraph(deltaTime);
	}

	/// <inheritdoc/>
	public void OnFixedUpdate(double deltaTime)
	{
		Processor.FixedUpdateGraph(deltaTime);
	}

	/// <summary>
	/// Starts the graph processor, wiring up the <see cref="GraphProcessor.OnGraphCompleted"/> callback to
	/// automatically end the ability instance when the graph finishes.
	/// </summary>
	/// <remarks>
	/// An activation started while the processor is still stopping, or while node code of the activation before it is
	/// still running - when the ability shares one behavior between activations - waits for both to finish before it
	/// takes the processor over, context and callback included. If that start then throws, the activation is canceled,
	/// as one whose start throws at once is.
	/// </remarks>
	/// <param name="context">The ability behavior context for the current activation.</param>
	/// <param name="variableOverrides">An optional callback to overwrite graph variable values with runtime data before
	/// the graph's entry node fires.</param>
	protected void StartGraph(AbilityBehaviorContext context, Action<Variables>? variableOverrides = null)
	{
		// Handed over now, the context and callback would serve the rest of the last activation's teardown and code,
		// and its completion would end this activation.
		if (Processor.GraphContext.StartMustWait)
		{
			Processor.GraphContext.PendingStart = () => StartAfterWaiting(context, variableOverrides);
			return;
		}

		Processor.GraphContext.SharedVariables = context.Owner.SharedVariables;
		Processor.GraphContext.ActivationContext = context;
		Processor.OnGraphCompleted = context.InstanceHandle.End;
		Processor.StartGraph(variableOverrides);
	}

	/// <summary>
	/// Stops the graph processor if it is currently running. Clears the <see cref="GraphProcessor.OnGraphCompleted"/>
	/// callback first to prevent re-entrant calls when <see cref="OnEnded"/> triggers during the stop cascade.
	/// </summary>
	protected void StopGraph()
	{
		Processor.OnGraphCompleted = null;

		// An activation that ends while the start it asked for still waits never gets its graph.
		Processor.GraphContext.PendingStart = null;

		if (Processor.GraphContext.HasStarted)
		{
			Processor.StopGraph();
		}
	}

	// OnStarted has returned by the time a start that waited runs, so the abilities system can no longer cancel the
	// activation when it throws, as it does for a start that throws at once. It is canceled here instead.
	private void StartAfterWaiting(AbilityBehaviorContext context, Action<Variables>? variableOverrides)
	{
		try
		{
			StartGraph(context, variableOverrides);
		}
		catch
		{
			context.InstanceHandle.AbilityInstance?.Cancel();
			throw;
		}
	}
}
