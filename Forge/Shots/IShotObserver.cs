// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Shots;

// Hears an entity's launches and the ends of its flights, as the server records them for its clients.
internal interface IShotObserver
{
	void Launched(EntityShots shots, in ProjectileFlight flight, bool ownersOwn);

	void Ended(EntityShots shots, in ProjectileFlight flight);
}
