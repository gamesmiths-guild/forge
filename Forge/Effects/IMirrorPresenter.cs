// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects;

// Hears of every application an entity a client mirrors refuses, so the client can show what it predicts of it.
internal interface IMirrorPresenter
{
	void Refused(IForgeEntity target, Effect effect, EffectApplicationContext? applicationContext);
}
