// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component that makes its target immune to incoming effects while the effect is active. Any effect matching
/// one of the <paramref name="immunityQueries"/> is denied before it can be applied, and the target's
/// <see cref="EffectsManager.OnEffectApplicationBlocked"/> event reports the denial.
/// </summary>
/// <remarks>
/// <para>
/// Only effects with duration can grant immunity, since the immunity lasts exactly as long as its effect is active.
/// Immunity is also <b>inhibition-aware</b>: while the effect is inhibited it blocks nothing, so a fire shield
/// suppressed by its own ongoing requirements stops turning fire away.
/// </para>
/// <para>
/// Immunity decides on arrival — it is consulted once, as an effect is applied — while
/// <see cref="TargetTagRequirementsEffectComponent"/> is reactive and re-evaluates on every tag change. Prefer the tag
/// requirements when the ignore-tag can be put on the incoming effects: they are simpler, they can suppress an effect
/// that is already active, and they have the ongoing/inhibition axis immunity lacks. Reach for immunity when the
/// incoming effects are not yours to edit, or when the decision depends on the effect's source, level, or modifiers
/// rather than on the target's tags.
/// </para>
/// <para>
/// An empty <see cref="EffectQuery"/> matches every effect, so an immunity built from one would block everything the
/// target could ever receive. That is treated as a misconfiguration rather than an intent: empty queries never block
/// anything and <see cref="EffectData"/> asserts against them.
/// </para>
/// <para>
/// This component maintains per-effect-instance state (its own handle and blocker registration). When used in
/// <see cref="EffectData"/>, each effect application will create its own instance via <see cref="CreateInstance"/> to
/// isolate state between different effect applications.
/// </para>
/// </remarks>
/// <param name="immunityQueries">The queries selecting which incoming effects to block. An effect matching any of them
/// is denied.</param>
public class ImmunityEffectComponent(EffectQuery[] immunityQueries) : IEffectComponent, IEffectApplicationBlocker
{
	private ActiveEffectHandle? _handle;

	internal EffectQuery[] ImmunityQueries { get; } = immunityQueries;

	// An empty query matches every effect, blocking everything the target could receive. EffectData rejects it.
	internal bool HasEmptyQuery => Array.Exists(ImmunityQueries, x => x.IsEmpty);

	/// <inheritdoc/>
	public IEffectComponent CreateInstance()
	{
		// Create a new instance for each effect application to isolate the handle and the blocker registration.
		return new ImmunityEffectComponent(ImmunityQueries);
	}

	/// <inheritdoc/>
	public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		_handle = activeEffectEvaluatedData.ActiveEffectHandle;
		target.EffectsManager.RegisterApplicationBlocker(this);

		return true;
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed,
		EffectRemovalReason reason)
	{
		if (!removed)
		{
			return;
		}

		target.EffectsManager.UnregisterApplicationBlocker(this);
		_handle = null;
	}

	/// <inheritdoc/>
	public bool AllowEffectApplication(in Effect effect)
	{
		// The effect can already be gone while its removal is still cascading — removal callbacks can apply further
		// effects — and an inhibited immunity is a suppressed one.
		if (_handle?.IsValid != true || _handle.IsInhibited)
		{
			return true;
		}

		foreach (EffectQuery query in ImmunityQueries)
		{
			if (!query.IsEmpty && query.Matches(effect))
			{
				return false;
			}
		}

		return true;
	}
}
