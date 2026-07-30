// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Statescript.Providers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AbilityActivatorResolverTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "AbilityActivator")]
	public void Resolver_value_type_is_the_ability_activator()
	{
		var resolver = new AbilityActivatorResolver(new ShoutProvider());

		resolver.ValueType.Should().Be(typeof(AbilityActivator));
	}

	[Fact]
	[Trait("Resolver", "AbilityActivator")]
	public void Activator_exposes_the_provider_data_type()
	{
		var resolver = new AbilityActivatorResolver(new ShoutProvider());

		resolver.Resolve(new GraphContext()).DataType.Should().Be(typeof(Shout));
	}

	[Fact]
	[Trait("Resolver", "AbilityActivator")]
	public void Activator_builds_data_from_the_current_graph_state_on_each_activation()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var captured = new List<Shout>();
		AbilityHandle handle = GrantTypedAbility(owner, "Shouter", captured, abilityTags: null);

		var graphContext = new GraphContext();
		graphContext.GraphVariables.DefineVariable("volume", 3);

		AbilityActivator activator = new AbilityActivatorResolver(new ShoutProvider()).Resolve(graphContext);

		activator.Activate(handle, null, 0f, graphContext).Should().BeTrue();
		graphContext.GraphVariables.SetVar("volume", 11);
		activator.Activate(handle, null, 0f, graphContext).Should().BeTrue();

		captured.Should().HaveCount(2);
		captured[0].Volume.Should().Be(3);
		captured[1].Volume.Should().Be(11);
	}

	[Fact]
	[Trait("Resolver", "AbilityActivator")]
	public void Activator_supplies_declared_input_values_to_the_provider()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var captured = new List<Shout>();
		AbilityHandle handle = GrantTypedAbility(owner, "Shouter", captured, abilityTags: null);

		var graphContext = new GraphContext();
		AbilityActivator activator = new AbilityActivatorResolver(
			new DeclaredInputShoutProvider(),
			new Dictionary<string, IPropertyResolver>
			{
				["Volume"] = new VariantResolver(new Variant128(9), typeof(int)),
			}).Resolve(graphContext);

		activator.Activate(handle, null, 0f, graphContext).Should().BeTrue();

		captured.Should().ContainSingle().Which.Volume.Should().Be(9);
	}

	[Fact]
	[Trait("Resolver", "AbilityActivator")]
	public void Activator_uses_default_input_value_when_no_resolver_is_bound()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var captured = new List<Shout>();
		AbilityHandle handle = GrantTypedAbility(owner, "Shouter", captured, abilityTags: null);

		var graphContext = new GraphContext();
		AbilityActivator activator =
			new AbilityActivatorResolver(new DeclaredInputShoutProvider()).Resolve(graphContext);

		activator.Activate(handle, null, 0f, graphContext).Should().BeTrue();

		captured.Should().ContainSingle().Which.Volume.Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "AbilityActivator")]
	public void Activator_activates_by_tag_and_ignores_abilities_that_do_not_accept_the_data()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		TagContainer abilityTags = new(_tagsManager, [Tag.RequestTag(_tagsManager, "color.red")]);

		var captured = new List<Shout>();
		GrantTypedAbility(owner, "Typed", captured, abilityTags);

		int untypedStarts = 0;
		var untypedData = new AbilityData(
			"Untyped",
			abilityTags: abilityTags,
			behaviorFactory: () => new CountingBehavior(() => untypedStarts++));
		owner.Abilities.GrantAbilityPermanently(untypedData, 1, LevelComparison.None, sourceEntity: null);

		var graphContext = new GraphContext();
		graphContext.GraphVariables.DefineVariable("volume", 5);

		AbilityActivator activator = new AbilityActivatorResolver(new ShoutProvider()).Resolve(graphContext);

		activator.ActivateByTag(owner.Abilities, abilityTags, null, graphContext).Should().BeTrue();

		captured.Should().ContainSingle().Which.Volume.Should().Be(5);
		untypedStarts.Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "AbilityActivator")]
	public void Activator_grants_and_activates_once_with_the_typed_data()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var captured = new List<Shout>();

		var abilityData = new AbilityData(
			"Proc",
			behaviorFactory: () => new ShoutBehavior(captured));

		var graphContext = new GraphContext();
		graphContext.GraphVariables.DefineVariable("volume", 42);

		AbilityActivator activator = new AbilityActivatorResolver(new ShoutProvider()).Resolve(graphContext);

		activator.GrantAndActivateOnce(
			owner.Abilities,
			abilityData,
			1,
			LevelComparison.None,
			null,
			null,
			graphContext).Should().BeTrue();

		captured.Should().ContainSingle().Which.Volume.Should().Be(42);

		// Transient grant: the ability is removed after it ends.
		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle).Should().BeFalse();
		handle.Should().BeNull();
	}

	[Fact]
	[Trait("Resolver", "AbilityActivator")]
	public void One_member_list_serves_both_directions()
	{
		// The same declaration authors the sending node's resolvers and offers the reading node's bindable fields.
		IReadOnlyList<AbilityActivationDataMember> members = new DeclaredInputShoutProvider().Members;

		members.Should().ContainSingle();
		members[0].Name.Should().Be("Volume");
		members[0].ValueType.Should().Be(typeof(int));
	}

	private static AbilityHandle GrantTypedAbility(
		TestEntity owner,
		string name,
		List<Shout> captured,
		TagContainer? abilityTags)
	{
		var abilityData = new AbilityData(
			name,
			abilityTags: abilityTags,
			behaviorFactory: () => new ShoutBehavior(captured));

		return owner.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, sourceEntity: null);
	}

	private sealed record Shout(int Volume);

	private sealed class ShoutProvider : AbilityActivationDataProvider<Shout>
	{
		public override Shout CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs)
		{
			graphContext.TryResolve("volume", out int volume);
			return new Shout(volume);
		}
	}

	private sealed class DeclaredInputShoutProvider : AbilityActivationDataProvider<Shout>
	{
		public override IReadOnlyList<AbilityActivationDataMember> Members =>
			[new AbilityActivationDataMember("Volume", typeof(int))];

		public override Shout CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs)
		{
			return new Shout(inputs.Get<int>("Volume"));
		}
	}

	private sealed class ShoutBehavior(List<Shout> captured) : IAbilityBehavior<Shout>
	{
		public void OnStarted(AbilityBehaviorContext context, Shout data)
		{
			captured.Add(data);
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// No-op.
		}
	}

	private sealed class CountingBehavior(Action onStarted) : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
			onStarted();
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// No-op.
		}
	}
}
