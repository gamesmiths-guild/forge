// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// An <see cref="IAbilityBehavior"/> implementation that drives an ability's lifecycle through a <see cref="Graph"/>.
/// When the ability starts, the graph begins processing; when the graph completes naturally (all state nodes
/// deactivate) or reaches an <see cref="Nodes.ExitNode"/>, the ability instance ends automatically. Canceling the
/// ability stops the graph immediately.
/// </summary>
/// <remarks>
/// <para>The entity's abilities advance the graph through <see cref="OnFixedUpdate"/> and <see cref="OnUpdate"/>. The
/// built-in timers count on the fixed rail, which <see cref="EntitySimulation.Tick(IForgeEntity, long, double)"/>
/// advances together with the entity's effects.</para>
/// </remarks>
/// <param name="graph">The graph definition to execute when the ability activates.</param>
public class GraphAbilityBehavior(Graph graph) : IAbilityBehavior, ISnapshotAbilityBehavior
{
	/// <summary>
	/// Gets the <see cref="GraphProcessor"/> that runs this behavior's graph.
	/// </summary>
	public GraphProcessor Processor { get; } = new GraphProcessor(graph);

	/// <inheritdoc/>
	public bool IsPresenting => Processor.IsPresenting;

	/// <inheritdoc/>
	bool ISnapshotAbilityBehavior.IsChanging => Processor.IsChanging;

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

	/// <inheritdoc/>
	public void OnPresent()
	{
		Processor.Present();
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
		Processor.GraphContext.Random = context.Owner.Random;
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

	/// <inheritdoc/>
#pragma warning disable SA1202 // Elements should be ordered by access
	object ISnapshotAbilityBehavior.CaptureSnapshot()
#pragma warning restore SA1202 // Elements should be ordered by access
	{
		return Processor.CaptureSnapshot();
	}

	/// <inheritdoc/>
	void ISnapshotAbilityBehavior.RestoreSnapshot(object snapshot)
	{
		Processor.SwapIn((GraphSnapshot)snapshot);
	}

	/// <inheritdoc/>
	void ISnapshotAbilityBehavior.DropRun()
	{
		Processor.DropRun();
	}

	/// <inheritdoc/>
	void ISnapshotAbilityBehavior.AddHolds(List<EntityHold> holds)
	{
		Processor.AddHolds(holds);
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
