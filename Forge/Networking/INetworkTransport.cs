// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// Moves bytes between the peers of a session. The core owns everything above it, the protocol, the clock, the
/// replication, and an engine provides one of these over its own sockets.
/// </summary>
/// <remarks>
/// <para>A transport is polled, never called back: the host takes what arrived with <see cref="TryReceive"/> at a
/// point of its own choosing, once per tick, so nothing changes the simulation from the middle of a socket callback,
/// and a test drives delivery step by step. An engine's transport buffers what its sockets hand it until then.</para>
/// <para>A send is either reliable, delivered in order with the other reliable sends on the same channel however long
/// it takes, or unreliable, delivered at most once, possibly late, possibly out of order, possibly never. The
/// protocol sends its connection control reliably and everything else unreliably, by design: an unreliable packet
/// that's late is worthless, and nothing should wait for it.</para>
/// <para>Peers are known by <see cref="PeerId"/>. The server is <see cref="PeerId.Server"/>, and a client sends only
/// to it: a transport is free to refuse anything else.</para>
/// </remarks>
public interface INetworkTransport
{
	/// <summary>
	/// Gets this machine's id in the session.
	/// </summary>
	PeerId LocalPeer { get; }

	/// <summary>
	/// Gets a value indicating whether this machine is the server.
	/// </summary>
	bool IsServer { get; }

	/// <summary>
	/// Gets the peers connected right now: every client on the server, the server on a client.
	/// </summary>
	IReadOnlyCollection<PeerId> Peers { get; }

	/// <summary>
	/// Sends a packet.
	/// </summary>
	/// <param name="to">Who to send it to.</param>
	/// <param name="channel">The channel to send it on, which keeps reliable packets in order with each other.
	/// </param>
	/// <param name="reliable">Whether to make sure it arrives, in order with other reliable packets on the channel.
	/// </param>
	/// <param name="payload">The bytes. The transport copies what it needs before returning.</param>
	void Send(PeerId to, byte channel, bool reliable, ReadOnlySpan<byte> payload);

	/// <summary>
	/// Takes the next thing that arrived, if anything did.
	/// </summary>
	/// <param name="transportEvent">What arrived.</param>
	/// <returns><see langword="true"/> if something was taken.</returns>
	bool TryReceive(out TransportEvent transportEvent);

	/// <summary>
	/// Disconnects a peer. On a client, the only peer is the server, so this leaves the session.
	/// </summary>
	/// <param name="peer">The peer to disconnect.</param>
	void Disconnect(PeerId peer);
}
