// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// Where a client stands with the server.
/// </summary>
public enum SessionState : byte
{
	/// <summary>
	/// Waiting for the transport to connect.
	/// </summary>
	Connecting = 0,

	/// <summary>
	/// Connected and waiting for the server to answer the hello.
	/// </summary>
	Handshaking = 1,

	/// <summary>
	/// Accepted: part of the session.
	/// </summary>
	Ready = 2,

	/// <summary>
	/// Turned away; see <see cref="NetworkSession.Refusal"/>.
	/// </summary>
	Refused = 3,

	/// <summary>
	/// The connection ended, by either side.
	/// </summary>
	Disconnected = 4,
}

/// <summary>
/// One end of a session: the server that every client talks to, or a client. It runs the handshake, answers or sends
/// pings for the clock, and moves input commands up from clients into per-client playout buffers. Everything above
/// it, what the ticks do with the input and what goes back down, comes with the later milestones.
/// </summary>
/// <remarks>
/// <para>The host calls <see cref="Update"/> once per tick, before simulating it, with a local time in seconds. The
/// session takes everything the transport received, acts on it, and on a client sends a ping when one is due. Nothing
/// happens between updates, so the simulation never sees the network change under it.</para>
/// <para>The server is <see cref="PeerId.Server"/> on its own transport. A listen host is a server whose own player
/// needs no connection: it reads that player's input locally. A dedicated server is a server with no viewer.</para>
/// <para>A client that is refused learns why through <see cref="Refusal"/> and leaves; the server disconnects it if it
/// hasn't after <see cref="RefusalLinger"/>. A packet that can't be read is dropped and counted, never acted on.</para>
/// </remarks>
public sealed class NetworkSession
{
	/// <summary>
	/// How often a client pings, in seconds.
	/// </summary>
	public const double DefaultPingInterval = 0.1;

	/// <summary>
	/// How long the server waits for a refused client to read why and leave, in seconds, before disconnecting it.
	/// </summary>
	public const double RefusalLinger = 2;

	/// <summary>
	/// How long the server waits for a connected client's hello, in seconds, before disconnecting it.
	/// </summary>
	public const double HandshakeTimeout = 5;

	private readonly Dictionary<PeerId, NetworkPeer> _peers = [];
	private readonly List<NetworkPeer> _readyPeers = [];
	private readonly List<RefusedPeer> _refused = [];
	private readonly List<PeerId> _silent = [];
	private readonly BitWriter _writer = new();
	private readonly BitReader _reader = new(ReadOnlyMemory<byte>.Empty);
	private readonly TagsManager _tagsManager;
	private readonly int _maxClients;
	private double _nextPingAt;
	private double _localTime;

	/// <summary>
	/// Raised on the server when a client's handshake succeeds and it joins the session.
	/// </summary>
	public event Action<NetworkPeer>? PeerJoined;

	/// <summary>
	/// Raised on the server when a client that had joined leaves, by disconnecting or being disconnected.
	/// </summary>
	public event Action<NetworkPeer>? PeerLeft;

	/// <summary>
	/// Raised on the server when a client is refused, with why.
	/// </summary>
	public event Action<PeerId, RefusalReason>? PeerRefused;

	/// <summary>
	/// Gets the transport.
	/// </summary>
	public INetworkTransport Transport { get; }

	/// <summary>
	/// Gets the session clock.
	/// </summary>
	public SimulationClock Clock { get; }

	/// <summary>
	/// Gets the asset registry.
	/// </summary>
	public NetworkAssetRegistry Registry { get; }

	/// <summary>
	/// Gets the input action set.
	/// </summary>
	public InputActionSet Actions { get; }

	/// <summary>
	/// Gets a value indicating whether this end is the server.
	/// </summary>
	public bool IsServer => Transport.IsServer;

	/// <summary>
	/// Gets where a client stands. The server is always <see cref="SessionState.Ready"/>.
	/// </summary>
	public SessionState State { get; private set; }

	/// <summary>
	/// Gets why the server refused this client, once <see cref="State"/> is <see cref="SessionState.Refused"/>.
	/// </summary>
	public RefusalReason? Refusal { get; private set; }

	/// <summary>
	/// Gets the seed the session's randomness derives from: the server's own, or the one a client was welcomed with.
	/// </summary>
	public ulong SessionSeed { get; private set; }

	/// <summary>
	/// Gets the clients that have joined, on the server.
	/// </summary>
	public IReadOnlyList<NetworkPeer> Peers => _readyPeers;

	/// <summary>
	/// Gets a client's clock sync: its estimate of the round trip and of the server's tick.
	/// </summary>
	public ClockSync ClockSync { get; }

	/// <summary>
	/// Gets a client's input window: the commands it sends the server, until the server acknowledges them.
	/// </summary>
	public InputWindow Input { get; }

	/// <summary>
	/// Gets the newest input tick the server has acknowledged to this client, or -1 before any.
	/// </summary>
	public long AckedInputTick { get; private set; } = -1;

	/// <summary>
	/// Gets how many input ticks the server held ahead of the one it last ran, the last the server said: zero means
	/// this client is late, a steady few is right, many means it runs too far ahead.
	/// </summary>
	public int ServerInputDepth { get; private set; }

	/// <summary>
	/// Gets or sets how often a client pings, in seconds.
	/// </summary>
	public double PingInterval { get; set; } = DefaultPingInterval;

	/// <summary>
	/// Gets how many packets couldn't be read and were dropped.
	/// </summary>
	public long MalformedPackets { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="NetworkSession"/> class over a transport. Whether it's the server
	/// or a client follows the transport.
	/// </summary>
	/// <param name="transport">The transport, connected or connecting.</param>
	/// <param name="clock">The session clock. On the server it's the session's time; a client's must run at the same
	/// rate, and is set to the server's tick when the server welcomes it.</param>
	/// <param name="registry">The sealed asset registry, whose hash the handshake compares.</param>
	/// <param name="tagsManager">The tags, whose hash the handshake compares, since input events travel as their net
	/// indices.</param>
	/// <param name="actions">The input action set, whose hash the handshake compares.</param>
	/// <param name="sessionSeed">On the server, the seed the session's randomness derives from, given to every
	/// client. Ignored on a client, which learns it from the server.</param>
	/// <param name="maxClients">On the server, how many clients to accept at most; 0 for no limit.</param>
	/// <exception cref="ArgumentException">Thrown when the registry isn't sealed.</exception>
	public NetworkSession(
		INetworkTransport transport,
		SimulationClock clock,
		NetworkAssetRegistry registry,
		TagsManager tagsManager,
		InputActionSet actions,
		ulong sessionSeed = 0,
		int maxClients = 0)
	{
		if (!registry.IsSealed)
		{
			throw new ArgumentException("Seal the registry before starting a session over it.", nameof(registry));
		}

		Transport = transport;
		Clock = clock;
		Registry = registry;
		_tagsManager = tagsManager;
		Actions = actions;
		SessionSeed = sessionSeed;
		_maxClients = maxClients;
		Input = new InputWindow(actions);
		ClockSync = new ClockSync(clock);
		State = transport.IsServer ? SessionState.Ready : SessionState.Connecting;
	}

	/// <summary>
	/// Finds a client by id, on the server.
	/// </summary>
	/// <param name="id">The client's id.</param>
	/// <returns>The client, or <see langword="null"/> if none with that id has joined.</returns>
	public NetworkPeer? FindPeer(PeerId id)
	{
		return _peers.TryGetValue(id, out NetworkPeer? peer) && peer.IsReady ? peer : null;
	}

	/// <summary>
	/// Takes everything the transport received and acts on it, and on a client sends a ping when one is due. Call it
	/// once per tick, before simulating the tick.
	/// </summary>
	/// <param name="localTime">The local time, in seconds, on any clock that only moves forward.</param>
	public void Update(double localTime)
	{
		_localTime = localTime;

		for (int i = _refused.Count - 1; i >= 0; i--)
		{
			if (_refused[i].DisconnectAt <= localTime)
			{
				Transport.Disconnect(_refused[i].Peer);
				_refused.RemoveAt(i);
			}
		}

		while (Transport.TryReceive(out TransportEvent received))
		{
			switch (received.Kind)
			{
				case TransportEventKind.PeerConnected:
					OnConnected(received.Peer);
					break;
				case TransportEventKind.PeerDisconnected:
					OnDisconnected(received.Peer);
					break;
				case TransportEventKind.Packet:
					OnPacket(received);
					break;
			}
		}

		// After the packets, so a hello that waited out a stall on the host is read first.
		if (IsServer)
		{
			DisconnectSilentPeers();
		}

		if (!IsServer && State == SessionState.Ready && localTime >= _nextPingAt)
		{
			_nextPingAt = localTime + PingInterval;
			_writer.Clear();
			new PingMessage(ClockSync.PingSent(localTime)).Write(_writer);
			Transport.Send(PeerId.Server, Protocol.ControlChannel, reliable: false, _writer.WrittenBytes);
		}
	}

	/// <summary>
	/// On a client, records the command for a tick and sends the window that now carries it.
	/// </summary>
	/// <param name="command">The command.</param>
	public void SendInput(in InputCommand command)
	{
		Input.Add(command);

		if (State != SessionState.Ready)
		{
			return;
		}

		_writer.Clear();
		Input.Write(_writer);
		Transport.Send(PeerId.Server, Protocol.InputChannel, reliable: false, _writer.WrittenBytes);
	}

	/// <summary>
	/// Leaves the session: a client disconnects from the server, a server disconnects every client.
	/// </summary>
	public void Disconnect()
	{
		if (IsServer)
		{
			// Refused clients, and ones whose connection the session hasn't taken yet, are connected too.
			foreach (PeerId id in new List<PeerId>(Transport.Peers))
			{
				Transport.Disconnect(id);
			}

			_refused.Clear();
			return;
		}

		Transport.Disconnect(PeerId.Server);
	}

	private void DisconnectSilentPeers()
	{
		foreach (NetworkPeer peer in _peers.Values)
		{
			if (!peer.IsReady && peer.ConnectedAt + HandshakeTimeout <= _localTime)
			{
				_silent.Add(peer.Id);
			}
		}

		foreach (PeerId id in _silent)
		{
			_peers.Remove(id);
			Transport.Disconnect(id);
		}

		_silent.Clear();
	}

	private void OnConnected(PeerId peer)
	{
		if (IsServer)
		{
			_peers[peer] = new NetworkPeer(peer, Actions, _localTime);
			return;
		}

		State = SessionState.Handshaking;
		_writer.Clear();
		new HelloMessage(Protocol.Version, Registry.Hash, Protocol.HashOf(_tagsManager), Protocol.HashOf(Actions))
			.Write(_writer);
		Transport.Send(PeerId.Server, Protocol.ControlChannel, reliable: true, _writer.WrittenBytes);
	}

	private void OnDisconnected(PeerId peer)
	{
		if (IsServer)
		{
			_refused.RemoveAll(refused => refused.Peer == peer);

			if (_peers.Remove(peer, out NetworkPeer? left) && left.IsReady)
			{
				left.IsReady = false;
				_readyPeers.Remove(left);
				PeerLeft?.Invoke(left);
			}

			return;
		}

		if (State != SessionState.Refused)
		{
			State = SessionState.Disconnected;
		}
	}

	private void OnPacket(in TransportEvent received)
	{
		try
		{
			_reader.Reset(received.Payload);

			if (IsServer)
			{
				OnServerPacket(received.Peer, received.Channel);
			}
			else
			{
				OnClientPacket(received.Channel);
			}
		}
		catch (MalformedPacketException)
		{
			MalformedPackets++;

			if (IsServer && _peers.TryGetValue(received.Peer, out NetworkPeer? peer))
			{
				peer.MalformedPackets++;
			}
		}
	}

	private void OnServerPacket(PeerId from, byte channel)
	{
		if (!_peers.TryGetValue(from, out NetworkPeer? peer))
		{
			return;
		}

		peer.LastHeardAt = _localTime;

		if (channel == Protocol.InputChannel)
		{
			if (peer.IsReady)
			{
				peer.Input.Receive(_reader);
			}

			return;
		}

		if (channel != Protocol.ControlChannel)
		{
			return;
		}

		var message = (ControlMessage)_reader.ReadByte();

		switch (message)
		{
			case ControlMessage.Hello when !peer.IsReady:
				OnHello(peer, HelloMessage.Read(_reader));
				break;
			case ControlMessage.Ping when peer.IsReady:
				OnPing(peer, PingMessage.Read(_reader));
				break;
			default:
				throw new MalformedPacketException($"A client doesn't send {message} now.");
		}
	}

	private void OnHello(NetworkPeer peer, HelloMessage hello)
	{
		RefusalReason? refusal = null;

		if (hello.Version != Protocol.Version)
		{
			refusal = RefusalReason.ProtocolVersion;
		}
		else if (hello.RegistryHash != Registry.Hash)
		{
			refusal = RefusalReason.AssetRegistry;
		}
		else if (hello.TagsHash != Protocol.HashOf(_tagsManager))
		{
			refusal = RefusalReason.Tags;
		}
		else if (hello.InputHash != Protocol.HashOf(Actions))
		{
			refusal = RefusalReason.InputActions;
		}
		else if (_maxClients > 0 && _readyPeers.Count >= _maxClients)
		{
			refusal = RefusalReason.Full;
		}

		_writer.Clear();

		if (refusal is RefusalReason reason)
		{
			new RefusedMessage(reason).Write(_writer);
			Transport.Send(peer.Id, Protocol.ControlChannel, reliable: true, _writer.WrittenBytes);
			PeerRefused?.Invoke(peer.Id, reason);
			_peers.Remove(peer.Id);
			_refused.Add(new RefusedPeer(peer.Id, _localTime + RefusalLinger));
			return;
		}

		new WelcomeMessage(Clock.TickRate, Clock.Tick, SessionSeed).Write(_writer);
		Transport.Send(peer.Id, Protocol.ControlChannel, reliable: true, _writer.WrittenBytes);
		peer.Input.StartAt(Clock.Tick);
		peer.IsReady = true;
		_readyPeers.Add(peer);
		PeerJoined?.Invoke(peer);
	}

	private void OnPing(NetworkPeer peer, PingMessage ping)
	{
		peer.Pings++;
		_writer.Clear();
		new PongMessage(ping.Sequence, Clock.Tick, peer.Input.NewestTick ?? -1, peer.Input.Depth).Write(_writer);
		Transport.Send(peer.Id, Protocol.ControlChannel, reliable: false, _writer.WrittenBytes);
	}

	private void OnClientPacket(byte channel)
	{
		if (channel != Protocol.ControlChannel)
		{
			return;
		}

		var message = (ControlMessage)_reader.ReadByte();

		switch (message)
		{
			case ControlMessage.Welcome when State == SessionState.Handshaking:
				OnWelcome(WelcomeMessage.Read(_reader));
				break;
			case ControlMessage.Refused when State == SessionState.Handshaking:
				Refusal = RefusedMessage.Read(_reader).Reason;
				State = SessionState.Refused;
				Transport.Disconnect(PeerId.Server);
				break;
			case ControlMessage.Pong when State == SessionState.Ready:
				OnPong(PongMessage.Read(_reader));
				break;
			default:
				throw new MalformedPacketException($"The server doesn't send {message} now.");
		}
	}

	private void OnWelcome(WelcomeMessage welcome)
	{
		if (welcome.TickRate != Clock.TickRate)
		{
			// The two ends can't simulate the same game at different rates; the client leaves rather than pretend.
			Refusal = RefusalReason.ProtocolVersion;
			State = SessionState.Refused;
			Transport.Disconnect(PeerId.Server);
			return;
		}

		SessionSeed = welcome.SessionSeed;
		Clock.Set(welcome.ServerTick);

		// What was recorded before is for ticks on the old clock.
		Input.Clear();
		AckedInputTick = -1;
		State = SessionState.Ready;
		_nextPingAt = _localTime;
	}

	private void OnPong(PongMessage pong)
	{
		ClockSync.PongReceived(pong.Sequence, pong.ServerTick, _localTime);
		ServerInputDepth = pong.InputDepth;

		if (pong.AckedInputTick > AckedInputTick)
		{
			AckedInputTick = pong.AckedInputTick;
			Input.Acknowledge(pong.AckedInputTick);
		}
	}

	private readonly record struct RefusedPeer(PeerId Peer, double DisconnectAt);
}
