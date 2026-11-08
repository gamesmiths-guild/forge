// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

// Hears of what an entity's graphs do that a look at them afterwards can't tell: the actions they execute.
internal interface IGraphObserver
{
	void ActionExecuted(GraphContext graphContext, Node node);
}
