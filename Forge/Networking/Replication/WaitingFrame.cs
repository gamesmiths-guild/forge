// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

// A frame an entity's input holds for a tick not simulated yet, put back after ticks that would have taken it run.
internal readonly record struct WaitingFrame(EntityInput Input, InputFrame? Frame)
{
	public static List<WaitingFrame> Of(IEnumerable<IForgeEntity> entities)
	{
		return [.. entities.Where(x => x.Input is not null).Select(x => new WaitingFrame(x.Input!, x.Input!.Waiting))];
	}

	public void PutBack()
	{
		Input.Waiting = Frame;
	}
}
