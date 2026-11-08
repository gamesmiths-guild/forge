// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes.State;

namespace Gamesmiths.Forge.Core;

// Something on an entity that holds on to something outside itself - an active effect or one of its components, a
// granted ability's trigger, a running state node of one of its graphs, a coded ability run - with the order it took
// hold in.
internal readonly record struct EntityHold(long Order, object Holder, Node? Node = null, int? Component = null)
{
	internal void Detach()
	{
		switch (Holder)
		{
			case ActiveEffect effect:
				effect.DetachForRestore(Component);
				break;

			case Ability ability:
				ability.DetachForRestore();
				break;

			case GraphProcessor processor:
				Node!.DetachForRestore(processor.GraphContext);
				break;

			case ISnapshotAbilityBehavior behavior:
				behavior.OnRestoreDetach();
				break;
		}
	}

	// The persistent cues the holder holds applied: an active effect's own, or a running Cue node's.
	internal void AddPersistentCues(List<PersistentCue> cues)
	{
		switch (Holder)
		{
			case ActiveEffect effect when Component is null:
				effect.AddPersistentCues(cues);
				break;

			case GraphProcessor processor when Node is CueNode cueNode:
				cueNode.AddPersistentCues(processor.GraphContext, cues);
				break;
		}
	}

	internal void Attach()
	{
		switch (Holder)
		{
			case ActiveEffect effect:
				effect.AttachForRestore(Component);
				break;

			case Ability ability:
				ability.AttachForRestore();
				break;

			case GraphProcessor processor:
				Node!.AttachForRestore(processor.GraphContext);
				break;

			case ISnapshotAbilityBehavior behavior:
				behavior.OnRestoreAttach();
				break;
		}
	}
}
