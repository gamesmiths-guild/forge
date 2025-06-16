// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Helpers;

public class TagsAndCuesFixture
{
	public TagsManager TagsManager { get; }

	public CuesManager CuesManager { get; }

	internal TestCue[] TestCueInstances { get; } = new TestCue[3];

	public TagsAndCuesFixture()
	{
		TagsManager = new TagsManager([
			"tag",
			"simple",
			"simple.tag",
			"other.tag",
			"enemy.undead.zombie",
			"enemy.undead.skeleton",
			"enemy.undead.ghoul",
			"enemy.undead.vampire",
			"enemy.beast.wolf",
			"enemy.beast.wolf.gray",
			"enemy.beast.boar",
			"enemy.humanoid.goblin",
			"enemy.humanoid.orc",
			"item.consumable.potion.health",
			"item.consumable.potion.mana",
			"item.consumable.potion.stamina",
			"item.consumable.food.apple",
			"item.consumable.food.bread",
			"item.equipment.weapon.sword",
			"item.equipment.weapon.dagger",
			"item.equipment.weapon.axe",
			"color.red",
			"color.green",
			"color.blue",
			"color.dark.red",
			"color.dark.green",
			"color.dark.blue",
			"test.cue1",
			"test.cue2",
			"test.cue3"
		]);

		CuesManager = new CuesManager();

		for (var i = 0; i < 3; i++)
		{
			var testCue = new TestCue();
			CuesManager.RegisterCue(Tag.RequestTag(TagsManager, $"test.cue{i + 1}"), testCue);
			TestCueInstances[i] = testCue;
		}
	}

	internal sealed class TestCue : ICueHandler
	{
		public event Action? OnApplied;

		public event Action? OnExecuted;

		public CueExecutionData ApplyData { get; private set; }

		public CueExecutionData ExecuteData { get; private set; }

		public CueExecutionData UpdateData { get; private set; }

		public bool Applied { get; private set; }

		public void Reset()
		{
			ApplyData = default;
			ExecuteData = default;
			UpdateData = default;
			Applied = false;
		}

		public void OnApply(IForgeEntity? target, CueParameters? parameters)
		{
			if (!parameters.HasValue)
			{
				return;
			}

			ApplyData = new CueExecutionData
			{
				Value = parameters.Value.Magnitude,
				NormalizedValue = parameters.Value.NormalizedMagnitude,
				Count = ApplyData.Count + 1,
				CustomParameters = parameters.Value.CustomParameters,
			};

			Applied = true;

			OnApplied?.Invoke();
		}

		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
			if (!parameters.HasValue)
			{
				return;
			}

			ExecuteData = new CueExecutionData
			{
				Value = parameters.Value.Magnitude,
				NormalizedValue = parameters.Value.NormalizedMagnitude,
				Count = ExecuteData.Count + 1,
				CustomParameters = parameters.Value.CustomParameters,
			};

			OnExecuted?.Invoke();
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
			Applied = false;
		}

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
		{
			Debug.Assert(Applied, "Cue must be applied before updating.");

			if (!parameters.HasValue)
			{
				return;
			}

			UpdateData = new CueExecutionData
			{
				Value = parameters.Value.Magnitude,
				NormalizedValue = parameters.Value.NormalizedMagnitude,
				Count = UpdateData.Count + 1,
				CustomParameters = parameters.Value.CustomParameters,
			};
		}

		public struct CueExecutionData
		{
			public int Value;
			public float NormalizedValue;
			public int Count;
			public Dictionary<StringKey, object>? CustomParameters;
		}
	}
}
