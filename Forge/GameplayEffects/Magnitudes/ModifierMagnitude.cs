// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// Provides a magnitude for the <see cref="Modifiers.Modifier"/> in a <see cref="GameplayEffect"/>.
/// TODO:
/// CustomCalculationClass
/// Setbycaller.
/// </summary>
public readonly struct ModifierMagnitude : IEquatable<ModifierMagnitude>
{
	/// <summary>
	/// Gets the type of magnitude calculation being used for this magnitude.
	/// </summary>
	public readonly MagnitudeCalculationType MagnitudeCalculationType { get; }

	/// <summary>
	/// Gets the <see cref="ScalableFloat"/> used for calculating this magnitude.
	/// </summary>
	/// <remarks>
	/// Is only valid if <see cref="MagnitudeCalculationType"/> == <see cref="MagnitudeCalculationType.ScalableFloat"/>.
	/// </remarks>
	public readonly ScalableFloat? ScalableFloatMagnitude { get; }

	/// <summary>
	/// Gets the <see cref="AttributeBasedFloat"/> used for calculating this magnitude.
	/// </summary>
	/// <remarks>
	/// Is only valid if <see cref="MagnitudeCalculationType"/> ==
	/// <see cref="MagnitudeCalculationType.AttributeBased"/>.
	/// </remarks>
	public readonly AttributeBasedFloat? AttributeBasedFloat { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ModifierMagnitude"/> struct.
	/// </summary>
	/// <param name="magnitudeCalculationType">The type of calculation used for this magnitude.</param>
	/// <param name="scalableFloatMagnitude">The scalable float used for this magnitude, if set as
	/// <see cref="MagnitudeCalculationType.ScalableFloat"/>.</param>
	/// <param name="attributeBasedFloat">The attribute based float used for this magnitude, if set as
	/// <see cref="MagnitudeCalculationType.AttributeBased"/>.</param>
	public ModifierMagnitude(
		MagnitudeCalculationType magnitudeCalculationType,
		ScalableFloat? scalableFloatMagnitude = null,
		AttributeBasedFloat? attributeBasedFloat = null)
	{
		Debug.Assert(
			(magnitudeCalculationType == MagnitudeCalculationType.ScalableFloat && scalableFloatMagnitude.HasValue
					&& !attributeBasedFloat.HasValue)
				|| (magnitudeCalculationType == MagnitudeCalculationType.AttributeBased && attributeBasedFloat.HasValue
					&& !scalableFloatMagnitude.HasValue),
			$"Invalid parameters for {nameof(magnitudeCalculationType)} == {magnitudeCalculationType}.");

		MagnitudeCalculationType = magnitudeCalculationType;
		ScalableFloatMagnitude = scalableFloatMagnitude;
		AttributeBasedFloat = attributeBasedFloat;
	}

	/// <summary>
	/// Gets the calculated magnitude for a given <see cref="GameplayEffect"/> and this <see cref="ModifierMagnitude"/>
	/// configurations.
	/// </summary>
	/// <param name="effect">The GameplayEffect to calculate the magnitude for.</param>
	/// <param name="target">The target which might be used for the magnitude calculation.</param>
	/// <param name="level">An optional custom level used for magnitude calculation. Will use the effect's level if not
	/// provided.</param>
	/// <returns>The evaluated magnitude.</returns>
	public readonly float GetMagnitude(GameplayEffect effect, IForgeEntity target, int? level = null)
	{
		switch (MagnitudeCalculationType)
		{
			case MagnitudeCalculationType.ScalableFloat:
				Debug.Assert(
					ScalableFloatMagnitude.HasValue,
					$"{nameof(ScalableFloatMagnitude)} should always have a value at this point.");
				return ScalableFloatMagnitude.Value.GetValue(level ?? effect.Level);

			case MagnitudeCalculationType.AttributeBased:
				Debug.Assert(
					AttributeBasedFloat.HasValue,
					$"{nameof(AttributeBasedFloat)} should always have a value at this point.");
				return AttributeBasedFloat.Value.CalculateMagnitude(effect, target, level ?? effect.Level);

			default:
				return 0;
		}
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(MagnitudeCalculationType);
		hash.Add(ScalableFloatMagnitude);
		hash.Add(AttributeBasedFloat);

		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is ModifierMagnitude other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(ModifierMagnitude other)
	{
		return MagnitudeCalculationType.Equals(other.MagnitudeCalculationType)
			&& Nullable.Equals(ScalableFloatMagnitude, other.ScalableFloatMagnitude)
			&& Nullable.Equals(AttributeBasedFloat, other.AttributeBasedFloat);
	}

	/// <summary>
	/// Determines if two <see cref="ModifierMagnitude"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="ModifierMagnitude"/> to compare.</param>
	/// <param name="rhs">The second <see cref="ModifierMagnitude"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(ModifierMagnitude lhs, ModifierMagnitude rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="ModifierMagnitude"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="ModifierMagnitude"/> to compare.</param>
	/// <param name="rhs">The second <see cref="ModifierMagnitude"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(ModifierMagnitude lhs, ModifierMagnitude rhs)
	{
		return !(lhs == rhs);
	}
}
