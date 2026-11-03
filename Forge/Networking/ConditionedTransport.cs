// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// Wraps a transport and treats what it sends the way a real link would: holds each packet for the latency plus some
/// jitter, loses a share of the unreliable ones, and keeps reliable ones in order. The network conditioner the plan
/// calls for, usable in tests and in a running game alike.
/// </summary>
/// <remarks>
/// <para>Time comes from the host, through <see cref="Update"/>, so a test can step it tick by tick and a game can
/// pass its own clock. Held packets are handed to the inner transport when their time comes, from
/// <see cref="Update"/>; nothing is sent from inside <see cref="Send"/>.</para>
/// <para>Draws come from an <see cref="IRandom"/>, so a <see cref="DeterministicRandom"/> makes a run repeatable. Only
/// the sending side is conditioned: wrap both ends of a connection to condition both directions.</para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="ConditionedTransport"/> class.
/// </remarks>
/// <param name="inner">The transport that moves the bytes.</param>
/// <param name="conditions">What to do to the packets. Changeable later through <see cref="Conditions"/>.</param>
/// <param name="random">Where the jitter and loss draws come from.</param>
public sealed class ConditionedTransport(INetworkTransport inner, NetworkConditions conditions, IRandom random)
	: INetworkTransport
{
	private readonly INetworkTransport _inner = inner;
	private readonly IRandom _random = random;
	private readonly List<HeldPacket> _held = [];
	private readonly Dictionary<ReliableChannel, double> _lastReliableDue = [];
	private double _now;
	private long _sequence;

	/// <summary>
	/// Gets or sets what is done to the packets sent from now on. Packets already held keep their timing.
	/// </summary>
	public NetworkConditions Conditions { get; set; } = conditions;

	/// <summary>
	/// Gets how many packets are held, waiting for their time to come.
	/// </summary>
	public int HeldPackets => _held.Count;

	/// <summary>
	/// Gets how many unreliable packets were lost so far.
	/// </summary>
	public long LostPackets { get; private set; }

	/// <inheritdoc/>
	public PeerId LocalPeer => _inner.LocalPeer;

	/// <inheritdoc/>
	public bool IsServer => _inner.IsServer;

	/// <inheritdoc/>
	public IReadOnlyCollection<PeerId> Peers => _inner.Peers;

	/// <inheritdoc/>
	public void Send(PeerId peer, byte channel, bool reliable, ReadOnlySpan<byte> payload)
	{
		NetworkConditions sendConditions = Conditions;

		if (!reliable && sendConditions.LossRate > 0 && _random.NextSingle() < sendConditions.LossRate)
		{
			LostPackets++;
			return;
		}

		double jitter = sendConditions.JitterSeconds > 0
			? ((_random.NextSingle() * 2) - 1) * sendConditions.JitterSeconds
			: 0;
		double due = _now + Math.Max(0, sendConditions.LatencySeconds + jitter);

		// A reliable channel delivers in order, so a packet never overtakes the reliable one sent before it.
		if (reliable)
		{
			var key = new ReliableChannel(peer, channel);

			if (_lastReliableDue.TryGetValue(key, out double previous))
			{
				due = Math.Max(due, previous);
			}

			_lastReliableDue[key] = due;
		}

		if (due <= _now)
		{
			_inner.Send(peer, channel, reliable, payload);
			return;
		}

		_held.Add(new HeldPacket(due, _sequence++, peer, channel, reliable, payload.ToArray()));
	}

	/// <inheritdoc/>
	public bool TryReceive(out TransportEvent transportEvent)
	{
		return _inner.TryReceive(out transportEvent);
	}

	/// <inheritdoc/>
	public void Disconnect(PeerId peer)
	{
		_held.RemoveAll(x => x.To == peer);
		_inner.Disconnect(peer);
	}

	/// <summary>
	/// Moves time forward and sends every held packet whose time has come, earliest first.
	/// </summary>
	/// <param name="now">The time, in seconds, on any clock that only moves forward.</param>
	public void Update(double now)
	{
		_now = Math.Max(_now, now);

		if (_held.Count == 0)
		{
			return;
		}

		// Earliest due first, and among equals the one sent first, so the order is the same in every run.
		_held.Sort(static (a, b) =>
		{
#pragma warning disable S1244 // Floating point numbers should not be tested for equality
			return a.Due != b.Due ? a.Due.CompareTo(b.Due) : a.Sequence.CompareTo(b.Sequence);
#pragma warning restore S1244 // Floating point numbers should not be tested for equality
		});

		int sent = 0;

		while (sent < _held.Count && _held[sent].Due <= _now)
		{
			HeldPacket packet = _held[sent];
			_inner.Send(packet.To, packet.Channel, packet.Reliable, packet.Payload);
			sent++;
		}

		_held.RemoveRange(0, sent);
	}

	private readonly record struct HeldPacket(
		double Due,
		long Sequence,
		PeerId To,
		byte Channel,
		bool Reliable,
		byte[] Payload);

	private readonly record struct ReliableChannel(PeerId Peer, byte Channel);
}
