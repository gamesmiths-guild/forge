// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Abilities;

/// <summary>
/// Covers the order an entity's abilities are listed and updated in: the order they were granted.
/// </summary>
/// <param name="fixture">The fixture that provides the tags and cues managers.</param>
public class GrantOrderTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Abilities", "Order")]
	public void Granted_abilities_are_listed_in_grant_order_after_a_removal()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var log = new List<string>();

		AbilityHandle a = Grant(entity, "a", log);
		AbilityHandle b = Grant(entity, "b", log);
		AbilityHandle c = Grant(entity, "c", log);

		entity.Abilities.RevokeAbility(b).Should().BeTrue();
		AbilityHandle d = Grant(entity, "d", log);

		entity.Abilities.GrantedAbilities.Should().Equal(
			[a, c, d],
			"removing b must not let d take its place");
	}

	[Fact]
	[Trait("Abilities", "Order")]
	public void Fixed_updates_reach_abilities_in_grant_order()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var log = new List<string>();

		AbilityHandle a = Grant(entity, "a", log);
		AbilityHandle b = Grant(entity, "b", log);
		AbilityHandle c = Grant(entity, "c", log);
		entity.Abilities.RevokeAbility(b).Should().BeTrue();
		AbilityHandle d = Grant(entity, "d", log);

		foreach (AbilityHandle handle in new[] { d, c, a })
		{
			handle.TryActivate(out _).Should().BeTrue();
		}

		entity.Abilities.FixedUpdateAbilities(1.0 / 60.0);

		log.Should().Equal("a", "c", "d");
	}

	private static AbilityHandle Grant(TestEntity entity, string name, List<string> log)
	{
		var abilityData = new AbilityData(name, behaviorFactory: () => new LoggingBehavior(name, log));
		return entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, entity);
	}

	private sealed class LoggingBehavior(string name, List<string> log) : IAbilityBehavior
	{
		private readonly string _name = name;

		private readonly List<string> _log = log;

		public void OnStarted(AbilityBehaviorContext context)
		{
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}

		public void OnFixedUpdate(double deltaTime)
		{
			_log.Add(_name);
		}
	}
}
