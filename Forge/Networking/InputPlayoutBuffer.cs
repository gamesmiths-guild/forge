// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// How a tick's input reached the server's simulation.
/// </summary>
public enum InputDelivery : byte
{
	/// <summary>
	/// The client's command for the tick had arrived.
	/// </summary>
	Received = 0,

	/// <summary>
	/// The command hadn't arrived, but a later tick's had, so this one was lost on the way or sent too late to
	/// matter: the tick ran with the last frame held, without presses or releases.
	/// </summary>
	Lost = 1,

	/// <summary>
	/// No command for this tick or any later one had arrived: the client is running late. The tick ran with the last
	/// frame held, without presses or releases, and the client should run further ahead.
	/// </summary>
	Late = 2,
}

/// <summary>
/// The server's queue of one client's input commands, by tick: it keeps the first copy of each tick the client's
/// redundant packets bring, hands the simulation the command for each tick in order, and fills the ticks that never
/// arrived.
/// </summary>
/// <remarks>
/// <para>A tick that was never received runs with the previous frame's held actions and no edges, the same rule
/// <see cref="EntityInput"/> applies to a tick nothing was submitted for, so the entity keeps doing what it was
/// doing. Whether the gap was a loss (a later tick had arrived) or lateness (nothing had) is reported, since the
/// first is the link's and the second is the client's lead being too short.</para>
/// <para><see cref="Depth"/>, how many ticks past the one last taken have arrived, is what tells a client how far
/// ahead it runs: steady at the margin is right, zero means late, growing means too far ahead.</para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="InputPlayoutBuffer"/> class.
/// </remarks>
/// <param name="actions">The action set the frames belong to.</param>
/// <param name="maxTicksAhead">How far ahead a command may be, in ticks; see <see cref="MaxTicksAhead"/>.</param>
public sealed class InputPlayoutBuffer(
	InputActionSet actions,
	int maxTicksAhead = InputPlayoutBuffer.DefaultMaxTicksAhead)
{
	/// <summary>
	/// The default <see cref="MaxTicksAhead"/>, about two seconds at 60 ticks a second.
	/// </summary>
	public const int DefaultMaxTicksAhead = 128;

	private readonly SortedDictionary<long, InputCommand> _commands = [];
	private readonly List<InputCommand> _read = [];
	private InputFrame _lastFrame;

	// A command must be for a tick after this one, and at most MaxTicksAhead after it.
	private long? _floor;

	/// <summary>
	/// Gets the action set the frames belong to.
	/// </summary>
	public InputActionSet Actions { get; } = actions;

	/// <summary>
	/// Gets how far ahead of the tick last taken, or of the tick before the one the client joined at, a command may
	/// be. Further is a malformed or absurd packet and is dropped.
	/// </summary>
	public int MaxTicksAhead { get; } = maxTicksAhead;

	/// <summary>
	/// Gets how many ticks after the one last taken have arrived.
	/// </summary>
	public int Depth => _commands.Count;

	/// <summary>
	/// Gets the newest tick that has arrived, to acknowledge to the client, or <see langword="null"/> before any.
	/// </summary>
	public long? NewestTick { get; private set; }

	/// <summary>
	/// Gets the tick last taken, or <see langword="null"/> before any.
	/// </summary>
	public long? LastTakenTick { get; private set; }

	/// <summary>
	/// Gets how many ticks were taken with a received command.
	/// </summary>
	public long ReceivedTicks { get; private set; }

	/// <summary>
	/// Gets how many ticks were filled because their command was lost.
	/// </summary>
	public long LostTicks { get; private set; }

	/// <summary>
	/// Gets how many ticks were filled because the client was late.
	/// </summary>
	public long LateTicks { get; private set; }

	/// <summary>
	/// Takes a packet an <see cref="InputWindow"/> wrote, keeping the first copy of each tick not yet taken.
	/// </summary>
	/// <param name="reader">The packet.</param>
	/// <returns>How many new ticks it brought.</returns>
	/// <exception cref="MalformedPacketException">Thrown when the packet can't be a window.</exception>
	public int Receive(BitReader reader)
	{
		InputWindow.Read(reader, Actions, _read);
		int added = 0;

		foreach (InputCommand command in _read)
		{
			added += Add(command) ? 1 : 0;
		}

		return added;
	}

	/// <summary>
	/// Adds one command, keeping the first copy of a tick not yet taken.
	/// </summary>
	/// <param name="command">The command.</param>
	/// <returns><see langword="true"/> if the tick was new; <see langword="false"/> if it was already here, already
	/// taken, or too far ahead.</returns>
	public bool Add(in InputCommand command)
	{
		if (_floor is long floor && (command.Tick <= floor || command.Tick > floor + MaxTicksAhead))
		{
			return false;
		}

		if (!_commands.TryAdd(command.Tick, command))
		{
			return false;
		}

		if (NewestTick is not long newest || command.Tick > newest)
		{
			NewestTick = command.Tick;
		}

		return true;
	}

	/// <summary>
	/// Takes the command for a tick, filling it if it never arrived. Ticks are taken in order; commands for ticks
	/// before this one are dropped.
	/// </summary>
	/// <param name="tick">The tick the server is about to simulate.</param>
	/// <param name="command">The command to run the tick with.</param>
	/// <returns>How the command reached the simulation.</returns>
	public InputDelivery Take(long tick, out InputCommand command)
	{
		// Ticks skipped over are gone: the simulation never ran them.
		while (_commands.Count > 0)
		{
			long oldest = FirstKey();

			if (oldest >= tick)
			{
				break;
			}

			_commands.Remove(oldest);
		}

		LastTakenTick = tick;
		_floor = tick;

		if (_commands.Remove(tick, out command))
		{
			_lastFrame = command.Frame;
			ReceivedTicks++;
			return InputDelivery.Received;
		}

		command = new InputCommand(tick, _lastFrame.WithoutEdges());
		_lastFrame = command.Frame;

		if (_commands.Count > 0)
		{
			LostTicks++;
			return InputDelivery.Lost;
		}

		LateTicks++;
		return InputDelivery.Late;
	}

	/// <summary>
	/// Forgets every command and the frame held, as when the client's clock is set to another tick.
	/// </summary>
	public void Clear()
	{
		_commands.Clear();
		_lastFrame = default;
		LastTakenTick = null;
		NewestTick = null;
		_floor = null;
	}

	/// <summary>
	/// Accepts commands from a tick on, before any is taken, so a client that just joined can't fill the buffer with
	/// ticks far ahead of the server.
	/// </summary>
	/// <param name="tick">The first tick the server may take.</param>
	internal void StartAt(long tick)
	{
		_floor = tick - 1;
	}

	private long FirstKey()
	{
		using SortedDictionary<long, InputCommand>.KeyCollection.Enumerator keys = _commands.Keys.GetEnumerator();
		return keys.MoveNext() ? keys.Current : long.MaxValue;
	}
}
