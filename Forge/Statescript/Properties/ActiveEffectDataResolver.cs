// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by reading a selected value from an <see cref="ActiveEffectHandle"/>.
/// </summary>
/// <remarks>
/// <para>Invalid or missing handles resolve to a default <see cref="Variant128"/> (zero or
/// <see langword="false"/>).</para>
/// <para>The produced value type depends on the selected <see cref="ActiveEffectDataType"/>: durations and the
/// period resolve as <see langword="double"/>, counts and level as <see langword="int"/>, and states as
/// <see langword="bool"/>.</para>
/// </remarks>
/// <param name="handleResolver">The resolver that produces the active effect handle to inspect.</param>
/// <param name="dataType">Which value to read from the active effect.</param>
public class ActiveEffectDataResolver(
	IObjectResolver<ActiveEffectHandle> handleResolver,
	ActiveEffectDataType dataType) : IPropertyResolver
{
	private readonly IObjectResolver<ActiveEffectHandle> _handleResolver = handleResolver
		?? throw new ArgumentNullException(nameof(handleResolver));

	private readonly ActiveEffectDataType _dataType = dataType;

	/// <inheritdoc/>
	public Type ValueType => _dataType switch
	{
		ActiveEffectDataType.StackCount => typeof(int),
		ActiveEffectDataType.Level => typeof(int),
		ActiveEffectDataType.ExecutionCount => typeof(int),
		ActiveEffectDataType.IsInhibited => typeof(bool),
		ActiveEffectDataType.IsValid => typeof(bool),
		_ => typeof(double),
	};

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		ActiveEffectHandle? handle = _handleResolver.Resolve(graphContext);

		if (handle is null)
		{
			return _dataType switch
			{
				ActiveEffectDataType.StackCount => new Variant128(0),
				ActiveEffectDataType.Level => new Variant128(0),
				ActiveEffectDataType.ExecutionCount => new Variant128(0),
				ActiveEffectDataType.IsInhibited => new Variant128(false),
				ActiveEffectDataType.IsValid => new Variant128(false),
				_ => new Variant128(0d),
			};
		}

		return _dataType switch
		{
			ActiveEffectDataType.RemainingDuration => new Variant128(handle.RemainingDuration),
			ActiveEffectDataType.TotalDuration => new Variant128(handle.TotalDuration),
			ActiveEffectDataType.RemainingFraction => new Variant128(GetRemainingFraction(handle)),
			ActiveEffectDataType.StackCount => new Variant128(handle.StackCount),
			ActiveEffectDataType.Level => new Variant128(handle.Level),
			ActiveEffectDataType.ExecutionCount => new Variant128(handle.ExecutionCount),
			ActiveEffectDataType.Period => new Variant128(handle.Period),
			ActiveEffectDataType.IsInhibited => new Variant128(handle.IsInhibited),
			ActiveEffectDataType.IsValid => new Variant128(handle.IsValid),
			_ => default,
		};
	}

	private static double GetRemainingFraction(ActiveEffectHandle handle)
	{
		if (!handle.IsValid)
		{
			return 0;
		}

		double totalDuration = handle.TotalDuration;

		if (totalDuration < 0)
		{
			// Infinite effects never run out.
			return 1;
		}

		if (totalDuration <= 0)
		{
			return 0;
		}

		return Math.Clamp(handle.RemainingDuration / totalDuration, 0, 1);
	}
}
