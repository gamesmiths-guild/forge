// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Core;

// Something on an entity that holds on to something outside itself - an active effect, a granted ability's trigger, a
// running state node of one of its graphs - with the order it took hold in.
internal readonly record struct EntityHold(long Order, object Holder, Node? Node = null)
{
	internal void Detach()
	{
		switch (Holder)
		{
			case ActiveEffect effect:
				effect.DetachForRestore();
				break;

			case Ability ability:
				ability.DetachForRestore();
				break;

			case GraphProcessor processor:
				Node!.DetachForRestore(processor.GraphContext);
				break;
		}
	}

	internal void Attach()
	{
		switch (Holder)
		{
			case ActiveEffect effect:
				effect.AttachForRestore();
				break;

			case Ability ability:
				ability.AttachForRestore();
				break;

			case GraphProcessor processor:
				Node!.AttachForRestore(processor.GraphContext);
				break;
		}
	}
}
