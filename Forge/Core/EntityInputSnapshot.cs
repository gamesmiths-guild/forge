// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

// An entity's input as an entity snapshot took it: the current frame, a frame waiting for the next tick, and how far
// the ticks have taken frames.
internal readonly record struct EntityInputSnapshot(
	InputFrame Current,
	InputFrame? Waiting,
	long? BegunTick,
	bool FrameTakenForAdvance,
	ulong FrameNumber);
