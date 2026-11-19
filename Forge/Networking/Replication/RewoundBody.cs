// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// An entity's body where an instigator was shown it, which the engine integration puts in place of the live one while
/// a compensated hit is tested.
/// </summary>
/// <param name="Entity">The entity.</param>
/// <param name="Body">Where its body was.</param>
public readonly record struct RewoundBody(IForgeEntity Entity, BodyState Body);
