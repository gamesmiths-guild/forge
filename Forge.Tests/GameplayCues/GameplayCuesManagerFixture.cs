// Copyright © 2025 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayCues;

namespace Gamesmiths.Forge.Tests.GameplayCues;

public class GameplayCuesManagerFixture
{
	public GameplayCuesManager GameplayCuesManager { get; }

	internal TestCue[] TestCueInstances { get; } = new TestCue[3];

	public GameplayCuesManagerFixture()
	{
		GameplayCuesManager = new GameplayCuesManager();

		for (var i = 0; i < 3; i++)
		{
			var testCue = new TestCue();
			GameplayCuesManager.RegisterCue($"Test.Cue{i + 1}", testCue);
			TestCueInstances[i] = testCue;
		}
	}

	internal class TestCue : IGameplayCue
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

		public void OnApply(IForgeEntity? target, GameplayCueParameters? parameters)
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
			};

			Applied = true;

			OnApplied?.Invoke();
		}

		public void OnExecute(IForgeEntity? target, GameplayCueParameters? parameters)
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
			};

			OnExecuted?.Invoke();
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
			Applied = false;
		}

		public void OnUpdate(IForgeEntity? target, GameplayCueParameters? parameters)
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
			};
		}

		public struct CueExecutionData
		{
			public int Value;
			public float NormalizedValue;
			public int Count;
		}
	}
}
