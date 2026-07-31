// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// A single condition on one of an entity's attributes, used by <see cref="AttributeRequirementsEffectComponent"/>.
/// It is the attribute-side counterpart of <see cref="TagRequirements"/>.
/// </summary>
/// <remarks>
/// <para>
/// <paramref name="MinValue"/> and <paramref name="MaxValue"/> form an inclusive range, and either one can be left out
/// to leave that side unbounded. Leaving out both makes the requirement vacuous, which <see cref="EffectData"/>
/// rejects.
/// </para>
/// <para>
/// A requirement naming an attribute the entity does not have is never met.
/// </para>
/// </remarks>
/// <param name="Attribute">The fully qualified key of the attribute to inspect.</param>
/// <param name="MinValue">The lowest accepted value, inclusive, or <see langword="null"/> for no lower bound.</param>
/// <param name="MaxValue">The highest accepted value, inclusive, or <see langword="null"/> for no upper bound.
/// </param>
/// <param name="ThresholdType">Whether the bounds are raw values or percentages of the attribute's maximum.</param>
/// <param name="CalculationType">Which value to read from the attribute.</param>
/// <param name="FinalChannel">The final channel, used only by
/// <see cref="AttributeCalculationType.MagnitudeEvaluatedUpToChannel"/>.</param>
public readonly record struct AttributeRequirement(
	StringKey Attribute,
	float? MinValue = null,
	float? MaxValue = null,
	AttributeThresholdType ThresholdType = AttributeThresholdType.Absolute,
	AttributeCalculationType CalculationType = AttributeCalculationType.CurrentValue,
	int FinalChannel = 0)
{
	/// <summary>
	/// Gets a value indicating whether this requirement has neither bound defined, and therefore constrains nothing.
	/// </summary>
	public bool IsEmpty => !MinValue.HasValue && !MaxValue.HasValue;

	/// <summary>
	/// Validates whether the given entity satisfies this requirement.
	/// </summary>
	/// <param name="entity">The entity whose attribute is inspected.</param>
	/// <returns><see langword="true"/> if the requirement is met; <see langword="false"/> otherwise.</returns>
	public bool RequirementMet(IForgeEntity entity)
	{
		if (IsEmpty)
		{
			return true;
		}

		if (!entity.Attributes.ContainsAttribute(Attribute))
		{
			return false;
		}

		EntityAttribute attribute = entity.Attributes[Attribute];
		float value = CalculationType.ResolveValue(attribute, FinalChannel);

		if (ThresholdType == AttributeThresholdType.PercentOfMax)
		{
			value = attribute.Max > 0 ? value / attribute.Max * 100f : 0f;
		}

		if (MinValue.HasValue && value < MinValue.Value)
		{
			return false;
		}

		return !MaxValue.HasValue || value <= MaxValue.Value;
	}

	/// <summary>
	/// Validates whether the given entity satisfies every requirement in a bucket.
	/// </summary>
	/// <remarks>
	/// An empty bucket constrains nothing, so it returns <paramref name="emptyResult"/>. Removal buckets pass
	/// <see langword="false"/> there, since "no removal conditions" must never read as "remove it". A
	/// <see langword="null"/> entity can satisfy nothing, so a non-empty bucket is never met against one.
	/// </remarks>
	/// <param name="requirements">The requirements to evaluate. All of them must be met.</param>
	/// <param name="entity">The entity to inspect, or <see langword="null"/> when there is none.</param>
	/// <param name="emptyResult">The result to report for an empty bucket.</param>
	/// <returns><see langword="true"/> if every requirement is met; <see langword="false"/> otherwise.</returns>
	internal static bool RequirementsMet(
		AttributeRequirement[] requirements,
		IForgeEntity? entity,
		bool emptyResult = true)
	{
		if (requirements.Length == 0)
		{
			return emptyResult;
		}

		if (entity is null)
		{
			return false;
		}

		foreach (AttributeRequirement requirement in requirements)
		{
			if (!requirement.RequirementMet(entity))
			{
				return false;
			}
		}

		return true;
	}
}
