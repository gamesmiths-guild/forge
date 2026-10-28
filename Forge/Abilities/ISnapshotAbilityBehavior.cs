// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

// A behavior whose run an entity snapshot can capture and restore: a Statescript graph. Its running nodes join the
// entity's holds, so they let go and take hold again in one order with the entity's effects and other graphs.
internal interface ISnapshotAbilityBehavior
{
	bool IsChanging { get; }

	object CaptureSnapshot();

	void RestoreSnapshot(object snapshot);

	void DropRun();

	void AddHolds(List<EntityHold> holds);
}
