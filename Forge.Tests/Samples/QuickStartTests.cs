// Copyright © Gamesmiths Guild.

#pragma warning disable

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Samples;

/// <summary>
/// The code of docs/quick-start.md, step by step. When this test needs a change, the guide does too.
/// </summary>
public class QuickStartTests
{
	[Fact]
	[Trait("Quick Start", null)]
	public void Hero_burns_and_heals()
	{
		// 2. Create the managers: one of each per game.
		var tagsManager = new TagsManager(["cue.burn", "cue.heal", "cooldown.heal"]);
		var cuesManager = new CuesManager();

		// 3. Define the hero.
		var hero = new Hero(tagsManager, cuesManager);
		hero.Attributes["HeroAttributes.Health"].CurrentValue.Should().Be(100);
		hero.Attributes["HeroAttributes.Mana"].CurrentValue.Should().Be(100);

		// 4. Set it on fire: 5 damage every half second, until removed.
		var burning = new EffectData(
			"Burning",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"HeroAttributes.Health",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-5))),
			],
			periodicData: new PeriodicData(
				Period: new ScalableFloat(0.5f),
				ExecuteOnApplication: true,
				PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod),
			cues:
			[
				new CueData(
					CueTags: tagsManager.RequestTagContainer(["cue.burn"]),
					MinValue: 0,
					MaxValue: 100,
					MagnitudeType: CueMagnitudeType.AttributeValueChange,
					MagnitudeAttribute: "HeroAttributes.Health"),
			]);

		// 6. Show it with a cue: the handler is registered before the effect is applied.
		var burnLog = new PrintCueHandler("Burn");
		cuesManager.RegisterCue(Tag.RequestTag(tagsManager, "cue.burn"), burnLog);

		ActiveEffectHandle? fire = hero.EffectsManager.ApplyEffect(
			new Effect(burning, new EffectOwnership(hero, hero)));

		// 5. Drive time.
		Tick(hero, 2f);
		hero.Attributes["HeroAttributes.Health"].CurrentValue.Should().Be(75);

		hero.EffectsManager.RemoveEffect(fire!);
		Tick(hero, 2f);
		hero.Attributes["HeroAttributes.Health"].CurrentValue.Should().Be(75);
		burnLog.Lines.Should().Equal(
			"Burn starts", "Burn: -5", "Burn: -5", "Burn: -5", "Burn: -5", "Burn: -5", "Burn ends");

		// 7. Give the hero a heal: an instant +30, a 25 Mana cost, and a 2 second cooldown tag.
		var heal = new EffectData(
			"Heal",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"HeroAttributes.Health",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(30))),
			]);

		var healCost = new EffectData(
			"Heal Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"HeroAttributes.Mana",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-25))),
			]);

		var healCooldown = new EffectData(
			"Heal Cooldown",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(2))),
			effectComponents: [new ModifierTagsEffectComponent(tagsManager.RequestTagContainer(["cooldown.heal"]))]);

		var healAbility = new AbilityData(
			name: "Heal",
			costEffect: healCost,
			cooldownEffects: [healCooldown],
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			behaviorFactory: () => new HealBehavior(heal));

		// 8. Grant it, then activate it whenever the player presses the key.
		AbilityHandle healHandle = hero.Abilities.GrantAbilityPermanently(
			healAbility,
			abilityLevel: 1,
			levelOverridePolicy: LevelComparison.None,
			sourceEntity: hero);

		healHandle.TryActivate(out AbilityActivationFailures failures).Should().BeTrue();
		hero.Attributes["HeroAttributes.Health"].CurrentValue.Should().Be(100);
		hero.Attributes["HeroAttributes.Mana"].CurrentValue.Should().Be(75);

		healHandle.TryActivate(out failures).Should().BeFalse();
		failures.Should().HaveFlag(AbilityActivationFailures.Cooldown);

		Tick(hero, 2f);
		healHandle.TryActivate(out failures).Should().BeTrue();
		hero.Attributes["HeroAttributes.Mana"].CurrentValue.Should().Be(50);
	}

	// The game loop's job: advance time for everything the entity owns.
	private static void Tick(Hero hero, float deltaTime)
	{
		hero.EffectsManager.UpdateEffects(deltaTime);
		hero.Abilities.UpdateAbilities(deltaTime);
	}

	public class HeroAttributes : AttributeSet
	{
		public EntityAttribute Health { get; }

		public EntityAttribute Mana { get; }

		public HeroAttributes()
		{
			// Current, minimum and maximum values.
			Health = InitializeAttribute(nameof(Health), 100, 0, 100);
			Mana = InitializeAttribute(nameof(Mana), 100, 0, 100);
		}
	}

	public class Hero : IForgeEntity
	{
		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public CuesManager CuesManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; }

		public Variables SharedVariables { get; } = new();

		public Hero(TagsManager tagsManager, CuesManager cuesManager)
		{
			Attributes = new EntityAttributes(this, new HeroAttributes());
			Tags = new EntityTags(new TagContainer(tagsManager));
			EffectsManager = new EffectsManager(this, cuesManager);
			CuesManager = cuesManager;
			Abilities = new EntityAbilities(this);
			Events = new EventManager();
		}
	}

	public class HealBehavior(EffectData heal) : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
			// Pays the cost and starts the cooldown; false when the hero cannot afford it.
			if (context.AbilityHandle.TryCommitAbility())
			{
				context.Owner.EffectsManager.ApplyEffect(
					new Effect(heal, new EffectOwnership(context.Owner, context.Owner)));
			}

			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}
	}

	public class PrintCueHandler(string name) : ICueHandler
	{
		// What the guide's version prints, kept so the test can check it.
		public List<string> Lines { get; } = [];

		public void OnApply(IForgeEntity? target, CueParameters? parameters)
		{
			Print($"{name} starts");
		}

		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
			Print($"{name}: {parameters?.Magnitude}");
		}

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
		{
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
			Print($"{name} ends");
		}

		private void Print(string line)
		{
			Lines.Add(line);
			Console.WriteLine(line);
		}
	}
}
