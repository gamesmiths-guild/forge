// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Converts between a raw integer and the decimal value it is meant to stand for, given how many decimal places are
/// packed into it.
/// </summary>
/// <remarks>
/// <para>
/// Storing a fractional quantity as a scaled integer — <c>475</c> for <c>4.75</c> — keeps arithmetic exactly
/// reproducible, at the cost of the stored number no longer reading as the number it represents. This converts between
/// the two, in both directions, and is <b>presentation only</b>: nothing in the simulation converts.
/// </para>
/// <para>
/// <see cref="Attributes.EntityAttribute"/> is the main user, through its own
/// <see cref="Attributes.EntityAttribute.DecimalPlaces"/>, but the conversion has nothing attribute-specific about it
/// and is public so that anything else keeping a scaled integer can use it too.
/// </para>
/// <para>
/// <b>When an <see cref="Attributes.EntityAttribute"/> is at hand, prefer its instance members over these.</b> The
/// number of places passed here is a copy of something the attribute already knows, and a copy can go stale: change
/// the attribute's declaration and every hard-coded call site keeps converting by the old scale, silently and wrongly.
/// These are for the cases with nothing to ask — a cue handler whose target came through null, editor tooling, a value
/// read back out of save data.
/// </para>
/// </remarks>
public static class Quantization
{
	/// <summary>
	/// The highest number of decimal places that can be packed into an <see langword="int"/>, since the scale they
	/// stand for is a power of ten that has to fit in one.
	/// </summary>
	public const int MaxDecimalPlaces = 9;

	private static readonly int[] _scales =
	[
		1,
		10,
		100,
		1_000,
		10_000,
		100_000,
		1_000_000,
		10_000_000,
		100_000_000,
		1_000_000_000
	];

	/// <summary>
	/// Gets the factor between a stored value and the value it stands for, which is 10 raised to
	/// <paramref name="decimalPlaces"/>.
	/// </summary>
	/// <param name="decimalPlaces">How many decimal places the stored integer stands for, from 0 to
	/// <see cref="MaxDecimalPlaces"/>.</param>
	/// <returns>The scale those places imply, which is 1 for no decimal places at all.</returns>
	public static int GetScale(int decimalPlaces)
	{
		return _scales[ClampDecimalPlaces(decimalPlaces)];
	}

	/// <summary>
	/// Converts a raw value into the value it stands for.
	/// </summary>
	/// <param name="rawValue">The stored value to convert.</param>
	/// <param name="decimalPlaces">How many decimal places that stored value stands for.</param>
	/// <returns>The value in display units.</returns>
	public static float ToDisplayValue(int rawValue, int decimalPlaces)
	{
		return (float)((double)rawValue / GetScale(decimalPlaces));
	}

	/// <summary>
	/// Formats a raw value the way it should be presented, with exactly <paramref name="decimalPlaces"/> decimals.
	/// </summary>
	/// <param name="rawValue">The stored value to format.</param>
	/// <param name="decimalPlaces">How many decimal places that stored value stands for.</param>
	/// <param name="formatProvider">The culture to format with.</param>
	/// <returns>The value as text, in display units.</returns>
	public static string ToDisplayString(int rawValue, int decimalPlaces, IFormatProvider formatProvider)
	{
		// The clamped count has to reach the format string as well as the scale: "F-1" is not a format, so an
		// out-of-range argument would throw here in a build where validation is off rather than degrade quietly.
		int places = ClampDecimalPlaces(decimalPlaces);

		return ToDisplayValue(rawValue, places).ToString($"F{places}", formatProvider);
	}

	/// <summary>
	/// Converts a value expressed in display units back into the raw integer that stores it, rounding halves away from
	/// zero.
	/// </summary>
	/// <param name="displayValue">The value in display units.</param>
	/// <param name="decimalPlaces">How many decimal places the stored value stands for.</param>
	/// <returns>The equivalent raw value, converted but not clamped to any bounds.</returns>
	public static int ToRawValue(float displayValue, int decimalPlaces)
	{
		double rawValue = Math.Round(
			(double)displayValue * GetScale(decimalPlaces),
			MidpointRounding.AwayFromZero);

		return (int)Math.Clamp(rawValue, int.MinValue, int.MaxValue);
	}

	/// <summary>
	/// Brings a decimal-place count into the supported range, reporting anything outside it as a validation failure
	/// first.
	/// </summary>
	/// <remarks>
	/// Out of range is a programming error, so validation is what should catch it. The clamp is what keeps a build
	/// with validation disabled coherent rather than crashing or reading a scale off the end of the table, and it is
	/// applied everywhere the count is used so that no two of its uses can disagree.
	/// </remarks>
	/// <param name="decimalPlaces">The count to bring into range.</param>
	/// <returns>The count, clamped between 0 and <see cref="MaxDecimalPlaces"/>.</returns>
	internal static int ClampDecimalPlaces(int decimalPlaces)
	{
		Validation.Assert(
			decimalPlaces >= 0 && decimalPlaces <= MaxDecimalPlaces,
			$"DecimalPlaces must be between 0 and {MaxDecimalPlaces}.");

		return Math.Clamp(decimalPlaces, 0, MaxDecimalPlaces);
	}
}
