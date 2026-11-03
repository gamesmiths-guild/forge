// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// What the server keeps about one connected client: who it is, the input it has sent, and how its connection is
/// doing.
/// </summary>
public sealed class NetworkPeer
{
	/// <summary>
	/// Gets the client's id.
	/// </summary>
	public PeerId Id { get; }

	/// <summary>
	/// Gets the client's input, tick by tick. The host takes each tick's command from it as it simulates the tick.
	/// </summary>
	public InputPlayoutBuffer Input { get; }

	/// <summary>
	/// Gets a value indicating whether the handshake is done and the client is part of the session.
	/// </summary>
	public bool IsReady { get; internal set; }

	/// <summary>
	/// Gets the local time the client connected at.
	/// </summary>
	public double ConnectedAt { get; }

	/// <summary>
	/// Gets the local time anything last arrived from the client.
	/// </summary>
	public double LastHeardAt { get; internal set; }

	/// <summary>
	/// Gets how many pings the client has sent.
	/// </summary>
	public long Pings { get; internal set; }

	/// <summary>
	/// Gets how many packets from the client couldn't be read and were dropped.
	/// </summary>
	public long MalformedPackets { get; internal set; }

	internal NetworkPeer(PeerId id, InputActionSet actions, double connectedAt)
	{
		Id = id;
		Input = new InputPlayoutBuffer(actions);
		ConnectedAt = connectedAt;
		LastHeardAt = connectedAt;
	}
}
