// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// The context for a <see cref="StateNode{T}"/>. This class holds the state of the node, such as whether it is active
/// or in the process of activating, as well as any deferred messages that need to be emitted after activation is
/// complete.
/// </summary>
public class StateNodeContext : INodeContext
{
	/// <summary>
	/// Gets a value indicating whether the node is active.
	/// </summary>
	public bool Active { get; internal set; }

	/// <summary>
	/// Gets a value indicating whether the in-progress deactivation was triggered by the Abort port. Reset on each
	/// activation; set just before deactivation when the node is aborted. Lets nodes distinguish a forced interruption
	/// from a natural shutdown (subgraph end or graph stop) inside <c>OnDeactivate</c>.
	/// </summary>
	public bool WasAborted { get; internal set; }

	internal bool Activating { get; set; }

	internal bool Deactivating { get; set; }

	internal int RunningFrames { get; set; }

	internal bool StartHeld { get; set; }

	internal ulong ActivationStamp { get; set; }

	// Unique within the graph context across runs, so the presentation pass can tell a running activation from one
	// that replaced it since the last pass. A restored snapshot counts from where it was taken, so running the same
	// activations again gives them the same ids.
	internal ulong ActivationId { get; set; }

	// When this activation took hold of what the node holds outside the graph, so an entity restore can take hold again
	// in the same order across the entity's graphs, effects and abilities.
	internal long HoldOrder { get; set; }

	internal int[]? DeferredDeactivationEventPortIds { get; set; }

	internal List<int> DeferredEmitMessageData { get; set; } = [];

	// A member-by-member copy for a graph snapshot, which every context type gets without code of its own.
	internal StateNodeContext Copy()
	{
		var copy = (StateNodeContext)MemberwiseClone();
		copy.DeferredEmitMessageData = [.. DeferredEmitMessageData];
		copy.OnCopied();
		return copy;
	}

	/// <summary>
	/// Called on a copy of this context that a graph snapshot has just made, member by member, either to keep or to
	/// restore. Until this returns, the copy shares every object the original holds. Give it a copy of its own of each
	/// one the node changes in place, such as a list it adds to.
	/// </summary>
	/// <remarks>
	/// Values need nothing, and neither do references the node only reads or replaces whole: an entity, an effect, an
	/// array it never writes into. Those stay shared, so a restored context points at the same objects. Overrides call
	/// the base first.
	/// </remarks>
	protected virtual void OnCopied()
	{
	}
}
