// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript;

/// <summary>
/// Covers where random nodes, random resolvers and the chance-to-apply component draw from: the graph's or the
/// entity's deterministic random when there is one, their own provider otherwise.
/// </summary>
/// <param name="fixture">The fixture that provides the tags and cues managers.</param>
public class GraphRandomTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Random", "Graph")]
	public void An_ability_graph_draws_from_its_owner_random()
	{
		var random = new DeterministicRandom(5UL);
		var entity = new TestEntity(_tagsManager, _cuesManager) { Random = random };

		GraphContext context = ResolverTestContextFactory.CreateAbilityGraphContext(entity);

		context.Random.Should().BeSameAs(random);
	}

	[Fact]
	[Trait("Random", "Graph")]
	public void An_ability_graph_has_no_random_when_its_owner_has_none()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		GraphContext context = ResolverTestContextFactory.CreateAbilityGraphContext(entity);

		context.Random.Should().BeNull();
	}

	[Fact]
	[Trait("Random", "Graph")]
	public void A_random_branch_rolls_from_the_graph_random_when_there_is_one()
	{
		var first = new List<string>();
		var second = new List<string>();

		RunRandomBranch(new ThrowingRandom(), Restarted(seed: 9UL, tick: 3), first);
		RunRandomBranch(new ThrowingRandom(), Restarted(seed: 9UL, tick: 3), second);

		first.Should().ContainSingle();
		second.Should().Equal(first, "the same seed and tick roll the same branch");
	}

	[Fact]
	[Trait("Random", "Graph")]
	public void A_random_branch_falls_back_to_its_provider_without_a_graph_random()
	{
		var provider = new CountingRandom();
		var log = new List<string>();

		RunRandomBranch(provider, graphRandom: null, log);

		provider.Calls.Should().Be(1);
		log.Should().ContainSingle();
	}

	[Fact]
	[Trait("Random", "Graph")]
	public void Random_resolvers_prefer_the_graph_random()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager) { Random = new DeterministicRandom(21UL) };
		GraphContext context = ResolverTestContextFactory.CreateAbilityGraphContext(entity);

		var minimum = new VariantResolver(new Variant128(0), typeof(int));
		var maximum = new VariantResolver(new Variant128(10), typeof(int));
		var elements = new ArrayResolver(minimum, maximum);
		var fallback = new ThrowingRandom();

		IPropertyResolver[] resolvers =
		[
			new RandomResolver(fallback, minimum, maximum),
			new RandomDirectionResolver(fallback),
			new RandomInsideCircleResolver(fallback),
			new RandomInsideSphereResolver(fallback),
			new RandomOnSphereResolver(fallback),
			new RandomElementResolver(elements, fallback),
		];

		foreach (IPropertyResolver resolver in resolvers)
		{
			resolver.Invoking(x => x.Resolve(context)).Should().NotThrow();
		}

		new ShuffleResolver(elements, fallback).Invoking(x => x.ResolveArray(context)).Should().NotThrow();
	}

	[Fact]
	[Trait("Random", "Graph")]
	public void Chance_to_apply_rolls_from_the_target_random()
	{
		var random = new DeterministicRandom(4UL);
		var target = new TestEntity(_tagsManager, _cuesManager) { Random = random };
		var component = new ChanceToApplyEffectComponent(new ThrowingRandom(), new ScalableFloat(0.5f));
		var effect = new Effect(
			new EffectData("Chance", new DurationData(DurationType.Instant)),
			new EffectOwnership(target, target));

		random.BeginTick(4);
		bool firstRoll = component.CanApplyEffect(target, effect);

		random.BeginTick(4);
		component.CanApplyEffect(target, effect).Should().Be(firstRoll, "the same tick rolls the same result");
	}

	private static DeterministicRandom Restarted(ulong seed, long tick)
	{
		var random = new DeterministicRandom(seed);
		random.BeginTick(tick);
		return random;
	}

	private static void RunRandomBranch(IRandom provider, IRandom? graphRandom, List<string> log)
	{
		var branch = new RandomBranchNode(provider);
		branch.BindInput(RandomBranchNode.ChanceInput, "chance");
		var onTrue = new TrackingActionNode("true", log);
		var onFalse = new TrackingActionNode("false", log);

		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("chance", 0.5);
		graph.AddNode(branch);
		graph.AddNode(onTrue);
		graph.AddNode(onFalse);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			branch.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			branch.OutputPorts[ConditionNode.TruePort],
			onTrue.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			branch.OutputPorts[ConditionNode.FalsePort],
			onFalse.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.GraphContext.Random = graphRandom;
		processor.StartGraph();
	}

	private sealed class CountingRandom : IRandom
	{
		private readonly DeterministicRandom _inner = new(1UL);

		public int Calls { get; private set; }

		public int NextInt()
		{
			return Count(_inner.NextInt());
		}

		public int NextInt(int maxValue)
		{
			return Count(_inner.NextInt(maxValue));
		}

		public int NextInt(int minValue, int maxValue)
		{
			return Count(_inner.NextInt(minValue, maxValue));
		}

		public int NextIntInclusive(int minValue, int maxValue)
		{
			return Count(_inner.NextIntInclusive(minValue, maxValue));
		}

		public float NextSingle()
		{
			return Count(_inner.NextSingle());
		}

		public float NextSingleInclusive()
		{
			return Count(_inner.NextSingleInclusive());
		}

		public double NextDouble()
		{
			return Count(_inner.NextDouble());
		}

		public double NextDoubleInclusive()
		{
			return Count(_inner.NextDoubleInclusive());
		}

		public long NextInt64()
		{
			return Count(_inner.NextInt64());
		}

		public long NextInt64(long maxValue)
		{
			return Count(_inner.NextInt64(maxValue));
		}

		public long NextInt64(long minValue, long maxValue)
		{
			return Count(_inner.NextInt64(minValue, maxValue));
		}

		public long NextInt64Inclusive(long minValue, long maxValue)
		{
			return Count(_inner.NextInt64Inclusive(minValue, maxValue));
		}

		public void NextBytes(byte[] buffer)
		{
			Calls++;
			_inner.NextBytes(buffer);
		}

		public void NextBytes(Span<byte> buffer)
		{
			Calls++;
			_inner.NextBytes(buffer);
		}

		private T Count<T>(T value)
		{
			Calls++;
			return value;
		}
	}

	private sealed class ThrowingRandom : IRandom
	{
		public int NextInt()
		{
			throw Used();
		}

		public int NextInt(int maxValue)
		{
			throw Used();
		}

		public int NextInt(int minValue, int maxValue)
		{
			throw Used();
		}

		public int NextIntInclusive(int minValue, int maxValue)
		{
			throw Used();
		}

		public float NextSingle()
		{
			throw Used();
		}

		public float NextSingleInclusive()
		{
			throw Used();
		}

		public double NextDouble()
		{
			throw Used();
		}

		public double NextDoubleInclusive()
		{
			throw Used();
		}

		public long NextInt64()
		{
			throw Used();
		}

		public long NextInt64(long maxValue)
		{
			throw Used();
		}

		public long NextInt64(long minValue, long maxValue)
		{
			throw Used();
		}

		public long NextInt64Inclusive(long minValue, long maxValue)
		{
			throw Used();
		}

		public void NextBytes(byte[] buffer)
		{
			throw Used();
		}

		public void NextBytes(Span<byte> buffer)
		{
			throw Used();
		}

		private static InvalidOperationException Used()
		{
			return new InvalidOperationException(
				"The fallback provider was used although a deterministic random exists.");
		}
	}
}
