// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;

namespace Gamesmiths.Forge.Tests.Helpers;

/// <summary>
/// A test <see cref="ICueHandler"/> that records every cue callback so tests can assert what was fired, with which
/// parameters/target, and how many times.
/// </summary>
internal sealed class RecordingCueHandler : ICueHandler
{
	public int ExecuteCount { get; private set; }

	public int ApplyCount { get; private set; }

	public int UpdateCount { get; private set; }

	public int RemoveCount { get; private set; }

	public bool IsApplied { get; private set; }

	public bool LastInterrupted { get; private set; }

	public IForgeEntity? LastTarget { get; private set; }

	public CueParameters? LastParameters { get; private set; }

	public void OnExecute(IForgeEntity? target, CueParameters? parameters)
	{
		ExecuteCount++;
		LastTarget = target;
		LastParameters = parameters;
	}

	public void OnApply(IForgeEntity? target, CueParameters? parameters)
	{
		ApplyCount++;
		IsApplied = true;
		LastTarget = target;
		LastParameters = parameters;
	}

	public void OnRemove(IForgeEntity? target, bool interrupted)
	{
		RemoveCount++;
		IsApplied = false;
		LastTarget = target;
		LastInterrupted = interrupted;
	}

	public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
	{
		UpdateCount++;
		LastTarget = target;
		LastParameters = parameters;
	}
}
