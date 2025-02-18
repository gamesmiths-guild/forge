// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayCues;

namespace Gamesmiths.Forge.Tests.GameplayCues;

public class GameplayCuesManagerFixture
{
	public GameplayCuesManager GameplayCuesManager { get; }

	public GameplayCuesManagerFixture()
	{
		GameplayCuesManager = new GameplayCuesManager();
	}
}
