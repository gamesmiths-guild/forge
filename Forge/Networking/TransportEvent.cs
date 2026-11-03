// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// What kind of thing a transport handed over.
/// </summary>
public enum TransportEventKind : byte
{
	/// <summary>
	/// A peer connected. On the server, a client joined; on a client, the connection to the server is up, and
	/// <see cref="TransportEvent.Peer"/> is <see cref="PeerId.Server"/>.
	/// </summary>
	PeerConnected = 0,

	/// <summary>
	/// A peer disconnected, or the connection to the server was lost.
	/// </summary>
	PeerDisconnected = 1,

	/// <summary>
	/// A packet arrived from <see cref="TransportEvent.Peer"/> on <see cref="TransportEvent.Channel"/>.
	/// </summary>
	Packet = 2,
}

/// <summary>
/// One thing a transport received: a peer coming or going, or a packet. Taken from the transport in order with
/// <see cref="INetworkTransport.TryReceive"/>.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Peer">The peer it concerns.</param>
/// <param name="Channel">The channel a packet arrived on; 0 for a peer event.</param>
/// <param name="Payload">A packet's bytes; empty for a peer event. Valid until the next
/// <see cref="INetworkTransport.TryReceive"/>, so a receiver that keeps them copies them.</param>
public readonly record struct TransportEvent(
	TransportEventKind Kind,
	PeerId Peer,
	byte Channel,
	ReadOnlyMemory<byte> Payload)
{
	/// <summary>
	/// A peer connected.
	/// </summary>
	/// <param name="peer">The peer.</param>
	/// <returns>The event.</returns>
	public static TransportEvent Connected(PeerId peer)
	{
		return new TransportEvent(TransportEventKind.PeerConnected, peer, 0, ReadOnlyMemory<byte>.Empty);
	}

	/// <summary>
	/// A peer disconnected.
	/// </summary>
	/// <param name="peer">The peer.</param>
	/// <returns>The event.</returns>
	public static TransportEvent Disconnected(PeerId peer)
	{
		return new TransportEvent(TransportEventKind.PeerDisconnected, peer, 0, ReadOnlyMemory<byte>.Empty);
	}

	/// <summary>
	/// A packet arrived.
	/// </summary>
	/// <param name="peer">Who sent it.</param>
	/// <param name="channel">The channel it came on.</param>
	/// <param name="payload">Its bytes.</param>
	/// <returns>The event.</returns>
	public static TransportEvent PacketFrom(PeerId peer, byte channel, ReadOnlyMemory<byte> payload)
	{
		return new TransportEvent(TransportEventKind.Packet, peer, channel, payload);
	}
}
