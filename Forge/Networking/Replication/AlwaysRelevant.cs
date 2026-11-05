// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// Every entity is relevant to every client.
/// </summary>
public sealed class AlwaysRelevant : IRelevancy
{
	/// <summary>
	/// The one instance.
	/// </summary>
	public static readonly AlwaysRelevant Instance = new();

	private AlwaysRelevant()
	{
	}

	/// <inheritdoc/>
	public bool IsRelevant(PeerId peer, ReplicatedEntity entity, ReplicationServer server)
	{
		return true;
	}
}
