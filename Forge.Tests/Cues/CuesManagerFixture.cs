// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;

namespace Gamesmiths.Forge.Tests.Cues;

public class CuesManagerFixture
{
	public CuesManager CuesManager { get; }

	internal TestCue[] TestCueInstances { get; } = new TestCue[3];

	public CuesManagerFixture()
	{
		CuesManager = new CuesManager();

		for (var i = 0; i < 3; i++)
		{
			var testCue = new TestCue();
			CuesManager.RegisterCue($"Test.Cue{i + 1}", testCue);
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
