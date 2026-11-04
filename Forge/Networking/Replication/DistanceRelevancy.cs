// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// An entity is relevant to a client while it's within a radius of any entity the client owns. An entity with no body,
/// or a client that owns nothing with a body, is relevant regardless.
/// </summary>
/// <param name="radius">The radius, in units.</param>
public sealed class DistanceRelevancy(float radius) : IRelevancy
{
	/// <summary>
	/// Gets or sets the radius, in units.
	/// </summary>
	public float Radius { get; set; } = radius;

	/// <inheritdoc/>
	public bool IsRelevant(PeerId peer, ReplicatedEntity entity, ReplicationServer server)
	{
		if (entity.Body is not BodyState body || entity.Owner == peer)
		{
			return true;
		}

		bool ownsABody = false;

		foreach (ReplicatedEntity other in server.Entities)
		{
			if (other.Owner != peer || other.Body is not BodyState viewpoint)
			{
				continue;
			}

			ownsABody = true;

			if (Vector3.DistanceSquared(viewpoint.Position, body.Position) <= Radius * Radius)
			{
				return true;
			}
		}

		return !ownsABody;
	}
}
