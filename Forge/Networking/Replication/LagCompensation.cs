// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// The server's memory of the world as its clients were shown it, so what a client's action hits is tested against
/// what its instigator saw: lag compensation. A client shows the entities it mirrors behind the server, by about half
/// a round trip and its interpolation delay, and every command it sends says by how much
/// (<see cref="InputCommand.ViewDelay"/>).
/// The server keeps every entity's body as each send carried it, and while a compensated hit is tested, the engine
/// integration puts the instigator's view of those bodies in place of the live ones (<see cref="Rewound"/>), between
/// <see cref="Begin"/> and <see cref="End"/>.
/// </summary>
/// <remarks>
/// <para>Only bodies are rewound. What an entity is, its tags, attributes and effects, is read as it is now, so a
/// shield raised after the attack still protects. An effect whose <see cref="EffectData.LagCompensation"/> is
/// <see cref="EffectLagCompensation.Rewound"/> counts as it was for whether a hit lands: while a scope is open, an
/// application's checks read the tags it grants and consult its immunity as they were at the instigator's tick.</para>
/// <para>An entity the instigator owns, which its client predicts and so shows where it is, isn't rewound; nor is one
/// whose <see cref="ReplicatedEntity.LagCompensated"/> is off. An instigator further behind than
/// <see cref="MaxRewindSeconds"/> is rewound that far and leads the rest.</para>
/// </remarks>
public sealed class LagCompensation
{
	private readonly SimulationClock _clock;
	private readonly Dictionary<PeerId, double> _viewDelays = [];
	private readonly Dictionary<IForgeEntity, Tracked> _tracked = new(EntityReferenceComparer.Instance);
	private readonly List<HeldEffect> _adjusted = [];

	/// <summary>
	/// Gets or sets how far back a hit may be tested, in seconds. An instigator further behind is rewound this far. The
	/// default is 0.5, which covers a round trip of about 250 ms in full: a client is shown the world its
	/// interpolation delay and its lead behind as well.
	/// </summary>
	public double MaxRewindSeconds { get; set; } = 0.5;

	/// <summary>
	/// Gets a value indicating whether a compensated hit is being tested, between <see cref="Begin"/> and
	/// <see cref="End"/>.
	/// </summary>
	public bool IsOpen { get; private set; }

	/// <summary>
	/// Gets the peer whose view the open scope tests against, or <see cref="PeerId.None"/> when none is open.
	/// </summary>
	public PeerId Instigator { get; private set; } = PeerId.None;

	/// <summary>
	/// Gets the tick the open scope tests against, which may fall between two ticks.
	/// </summary>
	public double ShownTick { get; private set; }

	internal LagCompensation(SimulationClock clock)
	{
		_clock = clock;
	}

	/// <summary>
	/// Gets how much of an action's compensation is left after it has run for a while: all of it on its first tick,
	/// none once it has run for the fade time, and in between in proportion. A fade time of 0 compensates the first
	/// tick alone, and an infinite one the whole of it, as for a projectile's whole flight.
	/// </summary>
	/// <param name="elapsedSeconds">How long the action has run: 0 on its first tick.</param>
	/// <param name="fadeSeconds">How long its compensation takes to fade out.</param>
	/// <returns>A fraction between 0 and 1.</returns>
	public static double Remaining(double elapsedSeconds, double fadeSeconds)
	{
		if (elapsedSeconds <= 0)
		{
			return 1;
		}

		if (fadeSeconds <= 0)
		{
			return 0;
		}

		return Math.Max(0, 1 - (elapsedSeconds / fadeSeconds));
	}

	/// <summary>
	/// Gets how many ticks behind the server a peer was shown the world in its newest command, capped at
	/// <see cref="MaxRewindSeconds"/>. 0 for a peer that sent none, and for the server's own.
	/// </summary>
	/// <param name="instigator">The peer.</param>
	/// <returns>The delay, in ticks.</returns>
	public double ViewDelay(PeerId instigator)
	{
		return _viewDelays.TryGetValue(instigator, out double delay)
			? Math.Min(delay, _clock.TicksIn(MaxRewindSeconds))
			: 0;
	}

	/// <summary>
	/// Gets the tick a peer was shown as it simulated a tick: the tick less its <see cref="ViewDelay"/>, scaled by how
	/// much compensation is left (<see cref="Remaining"/>).
	/// </summary>
	/// <param name="instigator">The peer.</param>
	/// <param name="tick">The tick being simulated.</param>
	/// <param name="remaining">How much of the compensation applies, from 0 to 1.</param>
	/// <returns>The tick the peer was shown, which may fall between two ticks.</returns>
	public double ShownTickOf(PeerId instigator, long tick, double remaining = 1)
	{
		return tick - (ViewDelay(instigator) * Math.Clamp(remaining, 0, 1));
	}

	/// <summary>
	/// Finds where an entity's body was at a tick, as its clients were shown it: between the states the sends on
	/// either side of it carried, as <see cref="RemoteBody.TrySample"/> finds it on a client.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <param name="tick">The tick, which may fall between two ticks.</param>
	/// <param name="state">The body at that tick.</param>
	/// <param name="snapDistance">How far apart two states may be, in units, and still be eased between, as the
	/// clients ease them.</param>
	/// <returns><see langword="false"/> for an entity no send carried a body of.</returns>
	public bool TryRewind(
		IForgeEntity entity,
		double tick,
		out BodyState state,
		float snapDistance = float.PositiveInfinity)
	{
		if (_tracked.TryGetValue(entity, out Tracked? tracked))
		{
			return tracked.Body.TrySample(tick, out state, snapDistance);
		}

		state = default;
		return false;
	}

	/// <summary>
	/// Lists the bodies a peer's actions are tested against, where it was shown them at a tick: every entity with a
	/// body that's lag compensated and that the peer doesn't own.
	/// </summary>
	/// <param name="instigator">The peer.</param>
	/// <param name="tick">The tick it was shown, which may fall between two ticks.</param>
	/// <param name="bodies">Where to put them. Cleared first.</param>
	/// <param name="snapDistance">How far apart two states may be, in units, and still be eased between, as the
	/// clients ease them.</param>
	public void Rewound(
		PeerId instigator,
		double tick,
		List<RewoundBody> bodies,
		float snapDistance = float.PositiveInfinity)
	{
		bodies.Clear();

		foreach (Tracked tracked in _tracked.Values)
		{
			ReplicatedEntity entity = tracked.Entity;

			// A placed entity is a mirror on its owner's client too, shown behind as any other.
			if ((entity.Owner == instigator && !entity.IsPlaced)
				|| !entity.LagCompensated
				|| entity.DespawnedAtTick is not null
				|| !tracked.Body.TrySample(tick, out BodyState body, snapDistance))
			{
				continue;
			}

			bodies.Add(new RewoundBody(entity.Entity, body));
		}
	}

	/// <summary>
	/// Opens a scope in which a peer's hit is tested against what it was shown at a tick. While it's open, an
	/// application to an entity reads its <see cref="EffectLagCompensation.Rewound"/> effects as they were at that
	/// tick. The engine integration puts the bodies of <see cref="Rewound"/> in place for its queries.
	/// </summary>
	/// <param name="instigator">The peer whose hit it is.</param>
	/// <param name="tick">The tick it was shown, usually from <see cref="ShownTickOf"/>.</param>
	/// <exception cref="InvalidOperationException">Thrown when a scope is open already.</exception>
	public void Begin(PeerId instigator, double tick)
	{
		if (IsOpen)
		{
			throw new InvalidOperationException("A lag compensation scope is open already.");
		}

		IsOpen = true;
		Instigator = instigator;
		ShownTick = tick;
	}

	/// <summary>
	/// Closes the scope <see cref="Begin"/> opened.
	/// </summary>
	public void End()
	{
		IsOpen = false;
		Instigator = PeerId.None;
		ShownTick = 0;
	}

	internal void NoteViewDelay(PeerId peer, double viewDelay)
	{
		_viewDelays[peer] = Math.Max(viewDelay, 0);
	}

	internal void ForgetPeer(PeerId peer)
	{
		_viewDelays.Remove(peer);
	}

	internal void Track(ReplicatedEntity entity)
	{
		var tracked = new Tracked(entity, this);
		_tracked[entity.Entity] = tracked;
		entity.Entity.EffectsManager.LagCompensation = this;

		// What it had as it was registered counts from then.
		foreach (ActiveEffect effect in entity.Entity.EffectsManager.ActiveEffects)
		{
			tracked.Hold(effect);
		}

		entity.Entity.EffectsManager.OnActiveEffectAdded += tracked.OnEffectAdded;
		entity.Entity.EffectsManager.OnActiveEffectRemoved += tracked.Removed;
	}

	internal void Untrack(ReplicatedEntity entity)
	{
		if (!_tracked.Remove(entity.Entity, out Tracked? tracked))
		{
			return;
		}

		entity.Entity.EffectsManager.LagCompensation = null;
		entity.Entity.EffectsManager.OnActiveEffectAdded -= tracked.OnEffectAdded;
		entity.Entity.EffectsManager.OnActiveEffectRemoved -= tracked.Removed;
	}

	// Notes what a send carried of an entity: its body, or none, which clears the body's history as a client clears it.
	internal void Record(ReplicatedEntity entity, long tick, BodyState? body)
	{
		if (!_tracked.TryGetValue(entity.Entity, out Tracked? tracked))
		{
			return;
		}

		long reach = (long)Math.Ceiling(_clock.TicksIn(MaxRewindSeconds));

		if (body is BodyState state)
		{
			// Enough states to reach that far back however often the server sends, and one before it to ease from.
			tracked.Body.MaxCount = (int)Math.Max(RemoteBody.Capacity, reach + 2);
			tracked.Body.Add(tick, state);
		}
		else
		{
			tracked.Body.Clear();
		}

		// An effect that ended before the furthest a hit reaches back can't count any more.
		long oldest = tick - reach - 1;
		tracked.Effects.RemoveAll(x => x.EndTick is long end && end < oldest);
	}

	// The effects of an entity that count as the instigator saw them and changed since: added since, to leave out, and
	// removed since, to put in, and the ones being removed, whatever they were. Empty outside a scope.
	internal IReadOnlyList<HeldEffect> Adjustments(
		IForgeEntity entity,
		IReadOnlyDictionary<ActiveEffect, int> removing)
	{
		_adjusted.Clear();

		if (!IsOpen || !_tracked.TryGetValue(entity, out Tracked? tracked))
		{
			return _adjusted;
		}

		foreach (HeldEffect effect in tracked.Effects)
		{
			if (WasThere(effect) != (effect.EndTick is null) || removing.ContainsKey(effect.Effect))
			{
				_adjusted.Add(effect);
			}
		}

		return _adjusted;
	}

	// Whether the effect was on its entity at the tick the open scope tests against.
	internal bool WasThere(HeldEffect effect)
	{
		return effect.StartTick <= ShownTick && (effect.EndTick is null || effect.EndTick > ShownTick);
	}

	// An effect that counts as the instigator saw it, with what it grants for an application's checks: its tags and its
	// immunity.
	internal sealed class HeldEffect(ActiveEffect effect, long startTick, TagContainer[] tags)
	{
		public ActiveEffect Effect { get; } = effect;

		public long StartTick { get; } = startTick;

		public long? EndTick { get; set; }

		public TagContainer[] Tags { get; } = tags;

		public IEffectApplicationBlocker[] Blockers { get; } =
			[.. effect.ComponentInstances.OfType<IEffectApplicationBlocker>()];

		public ImmunityEffectComponent[] Immunities { get; } =
			[.. effect.ComponentInstances.OfType<ImmunityEffectComponent>()];
	}

	private sealed class Tracked
	{
		private readonly LagCompensation _owner;

		public ReplicatedEntity Entity { get; }

		public Action<ActiveEffectHandle, EffectRemovalReason> Removed { get; }

		public RemoteBody Body { get; } = new();

		public List<HeldEffect> Effects { get; } = [];

		public Tracked(ReplicatedEntity entity, LagCompensation owner)
		{
			Entity = entity;
			_owner = owner;
			Removed = (handle, _) => OnEffectRemoved(handle);
		}

		public void OnEffectAdded(ActiveEffectHandle handle)
		{
			if (handle.ActiveEffect is ActiveEffect effect)
			{
				Hold(effect);
			}
		}

		public void Hold(ActiveEffect effect)
		{
			if (effect.EffectData.LagCompensation != EffectLagCompensation.Rewound)
			{
				return;
			}

			TagContainer[] tags =
				[.. effect.ComponentInstances.OfType<ModifierTagsEffectComponent>().Select(x => x.TagsToAdd)];
			Effects.Add(new HeldEffect(effect, _owner._clock.Tick, tags));
		}

		public void OnEffectRemoved(ActiveEffectHandle handle)
		{
			HeldEffect? held = Effects.Find(x => x.Effect == handle.ActiveEffect && x.EndTick is null);

			if (held is not null)
			{
				held.EndTick = _owner._clock.Tick;
			}
		}
	}
}
