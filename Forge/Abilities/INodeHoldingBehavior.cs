// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

// A behavior whose run holds through its running nodes rather than as one: a Statescript graph. Its nodes join the
// entity's holds each with its own order, so they let go and take hold again in one order with the entity's effects
// and other graphs.
internal interface INodeHoldingBehavior
{
	bool IsChanging { get; }

	void AddHolds(List<EntityHold> holds);
}
