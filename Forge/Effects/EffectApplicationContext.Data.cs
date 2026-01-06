// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Strongly-typed effect application context for passing custom data through the effect pipeline.
/// </summary>
/// <typeparam name="TData">The type of the context data.</typeparam>
/// <remarks>
/// Created automatically when using <see cref="EffectsManager.ApplyEffect{TData}(Effect, TData)"/>.
/// Access the data in CustomExecution or CustomModifierMagnitudeCalculator via
/// <see cref="EffectEvaluatedData.TryGetContextData{TData}(out TData)"/>.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="EffectApplicationContext{TData}"/> class.
/// </remarks>
/// <param name="data">The custom data for this effect application.</param>
public sealed class EffectApplicationContext<TData>(TData data) : EffectApplicationContext
{
	/// <summary>
	/// Gets the custom context data.
	/// </summary>
	public TData Data { get; } = data;
}
