// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a cooldown value (<see langword="float"/>) from an ability.
/// </summary>
/// <remarks>
/// <para>By default the resolver reads the ability driving the current graph through the active
/// <see cref="AbilityBehaviorContext"/>. Provide a handle resolver to inspect a different ability (for example one
/// produced by a GetAbilityHandle resolver).</para>
/// <para>When a cooldown tag is provided, only cooldown effects granting that tag are considered. Otherwise the
/// resolver reads the cooldown entry with the longest remaining time (falling back to the entry with the longest
/// total duration when the ability is not on cooldown).</para>
/// <para>Missing abilities resolve to <c>0</c>.</para>
/// </remarks>
/// <param name="dataType">Which cooldown value to read.</param>
/// <param name="cooldownTag">Optional tag filtering which cooldown to read.</param>
/// <param name="handleResolver">Optional resolver producing the ability handle to inspect.</param>
public class AbilityCooldownResolver(
	AbilityCooldownDataType dataType = AbilityCooldownDataType.RemainingTime,
	Tag? cooldownTag = null,
	IObjectResolver<AbilityHandle>? handleResolver = null) : IPropertyResolver
{
	private readonly AbilityCooldownDataType _dataType = dataType;
	private readonly Tag? _cooldownTag = cooldownTag;
	private readonly IObjectResolver<AbilityHandle>? _handleResolver = handleResolver;

	/// <inheritdoc/>
	public Type ValueType => typeof(float);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		AbilityHandle? handle = AbilityResolverUtilities.ResolveHandle(graphContext, _handleResolver);

		CooldownData[]? cooldownData = handle?.GetCooldownData();

		if (cooldownData is null || cooldownData.Length == 0)
		{
			return new Variant128(0f);
		}

		CooldownData? gatingCooldown = null;

		foreach (CooldownData data in cooldownData)
		{
			if (_cooldownTag.HasValue && data.CooldownTags?.HasTag(_cooldownTag.Value) != true)
			{
				continue;
			}

			// The gating cooldown is the one with the longest remaining time; when tied (typically both off
			// cooldown), the one with the longest total duration.
			if (gatingCooldown is null
				|| data.RemainingTime > gatingCooldown.Value.RemainingTime
				|| (data.RemainingTime >= gatingCooldown.Value.RemainingTime
					&& data.TotalTime > gatingCooldown.Value.TotalTime))
			{
				gatingCooldown = data;
			}
		}

		if (gatingCooldown is null)
		{
			return new Variant128(0f);
		}

		float remainingTime = gatingCooldown.Value.RemainingTime;
		float totalTime = gatingCooldown.Value.TotalTime;

		return _dataType switch
		{
			AbilityCooldownDataType.RemainingTime => new Variant128(remainingTime),
			AbilityCooldownDataType.TotalTime => new Variant128(totalTime),
			AbilityCooldownDataType.RemainingFraction => new Variant128(
				totalTime > 0 ? Math.Clamp(remainingTime / totalTime, 0f, 1f) : 0f),
			_ => default,
		};
	}
}
