// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayCues;

/// <summary>
/// A singleton class that manages the registration and execution of gameplay cues for an entity.
/// </summary>
public sealed class GameplayCuesManager
{
	private readonly Dictionary<StringKey, HashSet<IGameplayCue>> _activeCues = [];
	private readonly Dictionary<StringKey, HashSet<IGameplayCue>> _registeredCues = [];

	/// <summary>
	/// Registers a gameplay cue that can be triggered later.
	/// </summary>
	/// <param name="cueKey">The key for registering the cue.</param>
	/// <param name="cue">The cue to be registered to listen for the given key.</param>
	public void RegisterCue(StringKey cueKey, IGameplayCue cue)
	{
		if (_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? value))
		{
			value.Add(cue);
			return;
		}

		_registeredCues[cueKey] = [cue];
	}

	/// <summary>
	/// Unregisters a gameplay cue.
	/// </summary>
	/// <param name="cueKey">The key for the unregistered cue.</param>
	/// <param name="cue">The cue to be unregistered.</param>
	public void UnregisterCue(StringKey cueKey, IGameplayCue cue)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? value))
		{
			return;
		}

		value.Remove(cue);

		if (value.Count == 0)
		{
			_registeredCues.Remove(cueKey);
		}
	}

	/// <summary>
	/// Executes a one-shot gameplay cue.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void ExecuteCue(StringKey cueKey, IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? cues))
		{
			return;
		}

		foreach (IGameplayCue cue in cues)
		{
			cue.OnExecute(target, parameters);
		}
	}

	/// <summary>
	/// Adds a persistent gameplay cue.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void AddCue(StringKey cueKey, IForgeEntity? target, GameplayCueParameters parameters)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? cues))
		{
			return;
		}

		_activeCues.TryAdd(cueKey, []);

		foreach (IGameplayCue cue in cues)
		{
			cue.OnApply(target, parameters);
			_activeCues[cueKey].Add(cue);
		}
	}

	/// <summary>
	/// Removes a persistent gameplay cue.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="interrupted">Whether this removal is the result of an interruption.</param>
	public void RemoveCue(StringKey cueKey, IForgeEntity? target, bool interrupted)
	{
		if (!_activeCues.Remove(cueKey, out HashSet<IGameplayCue>? cues))
		{
			return;
		}

		foreach (IGameplayCue cue in cues)
		{
			cue.OnRemove(target, interrupted);
		}
	}
}
