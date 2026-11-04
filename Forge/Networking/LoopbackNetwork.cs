// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// A session's worth of transports in one process, for tests and for a listen host's own connection: packets go
/// straight from a sender's <see cref="INetworkTransport.Send"/> to the receiver's queue, and a
/// <see cref="ConditionedTransport"/> around a client adds the latency and loss a real link would.
/// </summary>
/// <remarks>
/// The server exists from the start. Each <see cref="Connect"/> adds a client, delivering a connected event to both
/// ends, and <see cref="INetworkTransport.Disconnect"/> from either end delivers a disconnected event to both.
/// </remarks>
public sealed class LoopbackNetwork
{
	private readonly Dictionary<PeerId, Endpoint> _endpoints = [];
	private int _nextClientId = PeerId.Server.Value + 1;

	/// <summary>
	/// Gets the server's transport.
	/// </summary>
	public INetworkTransport Server { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="LoopbackNetwork"/> class with its server.
	/// </summary>
	public LoopbackNetwork()
	{
		var server = new Endpoint(this, PeerId.Server);
		_endpoints.Add(PeerId.Server, server);
		Server = server;
	}

	/// <summary>
	/// Connects a new client, delivering a connected event to it and to the server.
	/// </summary>
	/// <returns>The client's transport.</returns>
	public INetworkTransport Connect()
	{
		var client = new Endpoint(this, new PeerId(_nextClientId++));
		_endpoints.Add(client.LocalPeer, client);

		Endpoint server = _endpoints[PeerId.Server];
		server.OnConnected(client.LocalPeer);
		client.OnConnected(PeerId.Server);

		return client;
	}

	private void Deliver(PeerId from, PeerId to, byte channel, ReadOnlySpan<byte> payload)
	{
		if (!_endpoints.TryGetValue(to, out Endpoint? endpoint) || !endpoint.IsConnectedTo(from))
		{
			return;
		}

		endpoint.Enqueue(TransportEvent.PacketFrom(from, channel, payload.ToArray()));
	}

	private void DisconnectClient(PeerId client)
	{
		if (!_endpoints.Remove(client, out Endpoint? endpoint))
		{
			return;
		}

		endpoint.OnDisconnected(PeerId.Server);
		_endpoints[PeerId.Server].OnDisconnected(client);
	}

	private sealed class Endpoint(LoopbackNetwork network, PeerId localPeer) : INetworkTransport
	{
		private readonly Queue<TransportEvent> _inbox = new();
		private readonly HashSet<PeerId> _peers = [];

		public PeerId LocalPeer { get; } = localPeer;

		public bool IsServer => LocalPeer.IsServer;

		public IReadOnlyCollection<PeerId> Peers => _peers;

		public void Send(PeerId peer, byte channel, bool reliable, ReadOnlySpan<byte> payload)
		{
			if (!_peers.Contains(peer))
			{
				return;
			}

			network.Deliver(LocalPeer, peer, channel, payload);
		}

		public bool TryReceive(out TransportEvent transportEvent)
		{
			return _inbox.TryDequeue(out transportEvent);
		}

		public void Disconnect(PeerId peer)
		{
			if (!_peers.Contains(peer))
			{
				return;
			}

			network.DisconnectClient(IsServer ? peer : LocalPeer);
		}

		internal bool IsConnectedTo(PeerId peer)
		{
			return _peers.Contains(peer);
		}

		internal void Enqueue(TransportEvent transportEvent)
		{
			_inbox.Enqueue(transportEvent);
		}

		internal void OnConnected(PeerId peer)
		{
			_peers.Add(peer);
			_inbox.Enqueue(TransportEvent.Connected(peer));
		}

		internal void OnDisconnected(PeerId peer)
		{
			_peers.Remove(peer);
			_inbox.Enqueue(TransportEvent.Disconnected(peer));
		}
	}
}
