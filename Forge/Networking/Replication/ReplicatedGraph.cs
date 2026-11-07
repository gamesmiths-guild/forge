// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Networking.Replication;

// A graph an ability of a replicated entity runs, for clients to run their copy of: what it is, which of the states
// clients run are running and since when, its value variables, and the actions clients run that it executed lately,
// each stamped with the send it was seen at. A variable still holding the value the graph starts with, like a state
// that never ran, has nothing to send.
internal sealed class ReplicatedGraph
{
	private const long Never = -1;

	private readonly List<RunningState> _scratch = [];

	public uint Instance { get; }

	public GraphProcessor Processor { get; }

	public GraphNetworkTable Table { get; }

	// The ability's index in the registry's Ability kind, or -1 when it isn't one a client can be told about.
	public int AssetIndex { get; }

	public uint TargetId { get; }

	public uint SourceId { get; }

	public int Level { get; }

	public float Magnitude { get; }

	public long CreatedStamp { get; private set; } = ReplicatedEntity.Pending;

	// The states clients run that are running, in the order the server started them.
	public List<RunningState> States { get; } = [];

	public long StatesStamp { get; private set; } = Never;

	public Variant128[] Values { get; }

	public long[] ValueStamps { get; }

	public Variant128[][] Arrays { get; }

	public long[] ArrayStamps { get; }

	public List<GraphEvent> Events { get; } = [];

	public long SeenAt { get; private set; } = -1;

	// The run ended, at the send stamped here, and stays until every client that could have it has heard.
	public bool Removed { get; private set; }

	public long RemovedStamp { get; private set; }

	public bool IsListed => AssetIndex >= 0;

	public ReplicatedGraph(uint instance, GraphProcessor processor, AbilityBehaviorContext context, int assetIndex)
	{
		Instance = instance;
		Processor = processor;
		Table = processor.Graph.NetworkTable;
		AssetIndex = assetIndex;
		TargetId = context.Target?.NetworkId.Value ?? 0;
		SourceId = context.Source?.NetworkId.Value ?? 0;
		Level = context.Level;
		Magnitude = context.Magnitude;

		GraphVariableDefinitions definitions = processor.Graph.VariableDefinitions;
		Values = [.. definitions.VariableDefinitions.Select(x => x.InitialValue)];
		ValueStamps = new long[Values.Length];
		Arrays = [.. definitions.ArrayVariableDefinitions.Select(x => x.InitialValues)];
		ArrayStamps = new long[Arrays.Length];
		Array.Fill(ValueStamps, Never);
		Array.Fill(ArrayStamps, Never);
	}

	// Stamps what changed since the last look with this send's tick.
	public void Capture(long tick)
	{
		SeenAt = tick;

		if (!IsListed)
		{
			return;
		}

		if (CreatedStamp == ReplicatedEntity.Pending)
		{
			CreatedStamp = tick;
		}

		CaptureStates(tick);
		CaptureVariables(tick);
	}

	public void Remove(long tick)
	{
		Removed = true;
		RemovedStamp = tick;
	}

	public bool ValuesChanged(long acked)
	{
		foreach (long stamp in ValueStamps)
		{
			if (stamp > acked)
			{
				return true;
			}
		}

		foreach (long stamp in ArrayStamps)
		{
			if (stamp > acked)
			{
				return true;
			}
		}

		return false;
	}

	private static bool SameBits(Variant128 first, Variant128 second)
	{
		return first.AsULong() == second.AsULong() && first.AsUpperULong() == second.AsUpperULong();
	}

	private void CaptureStates(long tick)
	{
		GraphContext context = Processor.GraphContext;
		_scratch.Clear();

		for (int i = 0; i < Table.States.Length; i++)
		{
			if (Table.States[i].TryGetActivation(context, out ulong activationId))
			{
				_scratch.Add(new RunningState(i, activationId, Since(i, activationId, tick)));
			}
		}

		_scratch.Sort(static (x, y) => x.ActivationId.CompareTo(y.ActivationId));
		bool changed = _scratch.Count != States.Count;

		for (int i = 0; !changed && i < _scratch.Count; i++)
		{
			changed = _scratch[i] != States[i];
		}

		if (!changed)
		{
			return;
		}

		States.Clear();
		States.AddRange(_scratch);
		StatesStamp = tick;
	}

	// The tick an activation was first seen at: the send it started before.
	private long Since(int state, ulong activationId, long tick)
	{
		foreach (RunningState running in States)
		{
			if (running.State == state && running.ActivationId == activationId)
			{
				return running.SinceTick;
			}
		}

		return tick;
	}

	private void CaptureVariables(long tick)
	{
		Variables variables = Processor.GraphContext.GraphVariables;

		for (int i = 0; i < Values.Length; i++)
		{
			variables.TryGetVariant(Table.ValueVariables[i], out Variant128 value);

			if (!SameBits(Values[i], value))
			{
				Values[i] = value;
				ValueStamps[i] = tick;
			}
		}

		for (int i = 0; i < Arrays.Length; i++)
		{
			StringKey name = Table.ArrayVariables[i];
			int length = Math.Max(variables.GetArrayLength(name), 0);
			Variant128[] array = Arrays[i];
			bool changed = array.Length != length;

			for (int j = 0; !changed && j < length; j++)
			{
				variables.TryGetArrayVariant(name, j, out Variant128 element);
				changed = !SameBits(array[j], element);
			}

			if (!changed)
			{
				continue;
			}

			array = new Variant128[length];

			for (int j = 0; j < length; j++)
			{
				variables.TryGetArrayVariant(name, j, out array[j]);
			}

			Arrays[i] = array;
			ArrayStamps[i] = tick;
		}
	}

	// A state clients run, running as the activation the server numbered, first seen at a send.
	internal readonly record struct RunningState(int State, ulong ActivationId, long SinceTick);

	// An action clients run, executed on the server, numbered in the entity's sequence of events, after the graph's
	// activations up to the one numbered here, so a client runs it between the same states.
	internal readonly record struct GraphEvent(uint Sequence, int Action, ulong AfterActivation, long CreatedAtTick);
}
