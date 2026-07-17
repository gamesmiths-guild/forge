// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see langword="float"/> by sampling an <see cref="ICurve"/> at a resolved position.
/// </summary>
/// <remarks>
/// Engine curve assets (such as Godot curves) plug in through their existing <see cref="ICurve"/> adapters — the
/// same abstraction scalable magnitudes use.
/// </remarks>
/// <param name="curve">The curve to sample.</param>
/// <param name="time">The resolver for the sample position.</param>
public class CurveSampleResolver(ICurve curve, IPropertyResolver time) : IPropertyResolver
{
	private readonly ICurve _curve = curve ?? throw new ArgumentNullException(nameof(curve));

	private readonly IPropertyResolver _time = time;

	/// <inheritdoc/>
	public Type ValueType => typeof(float);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		float floatTime = MathTypeUtils.ResolveAsFloat(_time.ValueType, _time.Resolve(graphContext));

		return new Variant128(_curve.Evaluate(floatTime));
	}
}
