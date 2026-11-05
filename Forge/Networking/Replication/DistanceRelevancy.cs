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
	// Where the peer's owned bodies were at the last send: a send asks about every entity for each client in turn.
	private readonly List<Vector3> _viewpoints = [];
	private PeerId _viewpointsPeer;
	private long _viewpointsTick = -1;

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

		if (peer != _viewpointsPeer || server.LastSendTick != _viewpointsTick)
		{
			CollectViewpoints(peer, server);
		}

		foreach (Vector3 viewpoint in _viewpoints)
		{
			if (Vector3.DistanceSquared(viewpoint, body.Position) <= Radius * Radius)
			{
				return true;
			}
		}

		return _viewpoints.Count == 0;
	}

	private void CollectViewpoints(PeerId peer, ReplicationServer server)
	{
		_viewpoints.Clear();
		_viewpointsPeer = peer;
		_viewpointsTick = server.LastSendTick;

		foreach (ReplicatedEntity other in server.Entities)
		{
			if (other.Owner == peer && other.Body is BodyState viewpoint)
			{
				_viewpoints.Add(viewpoint.Position);
			}
		}
	}
}
