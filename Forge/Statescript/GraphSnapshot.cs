// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// A graph run as it stood at one moment, taken by <see cref="GraphProcessor.CaptureSnapshot"/> and put back by
/// <see cref="GraphProcessor.RestoreSnapshot"/>.
/// </summary>
/// <remarks>
/// <para>It holds which state nodes were running, a copy of every node context and of the graph variables, the count
/// that numbers activations, and what the run was started with: its shared variables, activation context, random and
/// completion callback. A restore puts copies of those copies in, so nothing the graph does afterwards changes the
/// snapshot, and it can be restored any number of times.</para>
/// <para>What lies outside the graph is referenced, not copied: the entities, effects and engine objects the nodes
/// point at, the shared variables themselves, and the objects held in object variables. Bringing those back as they
/// were is the job of whatever restores the world.</para>
/// </remarks>
public sealed class GraphSnapshot
{
	internal GraphProcessor Processor { get; }

	internal bool HasStarted { get; }

	internal ulong ActivationCount { get; }

	// In node order, with the contexts of state nodes only: the others are caches their node rebuilds when it needs
	// them.
	internal NodeContextCopy[] NodeContexts { get; }

	internal Node[] ActiveNodes { get; }

	internal KeyValuePair<Guid, bool>[] ActivationStatus { get; }

	internal Variables GraphVariables { get; }

	internal Variables? SharedVariables { get; }

	internal object? ActivationContext { get; }

	internal IRandom? Random { get; }

	internal Action? OnGraphCompleted { get; }

	internal GraphSnapshot(
		GraphProcessor processor,
		bool hasStarted,
		ulong activationCount,
		NodeContextCopy[] nodeContexts,
		Node[] activeNodes,
		KeyValuePair<Guid, bool>[] activationStatus,
		Variables graphVariables,
		Variables? sharedVariables,
		object? activationContext,
		IRandom? random,
		Action? onGraphCompleted)
	{
		Processor = processor;
		HasStarted = hasStarted;
		ActivationCount = activationCount;
		NodeContexts = nodeContexts;
		ActiveNodes = activeNodes;
		ActivationStatus = activationStatus;
		GraphVariables = graphVariables;
		SharedVariables = sharedVariables;
		ActivationContext = activationContext;
		Random = random;
		OnGraphCompleted = onGraphCompleted;
	}

	internal readonly record struct NodeContextCopy(Node Node, StateNodeContext Context);
}
