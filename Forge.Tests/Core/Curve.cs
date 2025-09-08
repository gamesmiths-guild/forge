// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tests.Core;

/// <summary>
/// Initializes a new instance of the <see cref="Curve"/> class.
/// </summary>
/// <param name="Keys">The keys for the curve.</param>
public readonly record struct Curve(CurveKey[] Keys) : ICurve
{
	private readonly CurveKey[] _keys = [.. Keys.OrderBy(x => x.Time)];

	/// <summary>
	/// Evaluates the curve at a given time.
	/// </summary>
	/// <param name="value">The value for evaluation.</param>
	/// <returns>The evaluated value.</returns>
	public float Evaluate(float value)
	{
		if (_keys is null || _keys.Length == 0)
		{
			// Default scaling factor if no keys are defined.
			return 1.0f;
		}

		if (value <= _keys[0].Time)
		{
			return _keys[0].Value;
		}

		if (value >= _keys[^1].Time)
		{
			return _keys[^1].Value;
		}

		for (var i = 0; i < _keys.Length - 1; i++)
		{
			if (value >= _keys[i].Time && value <= _keys[i + 1].Time)
			{
				var t = (value - _keys[i].Time) / (_keys[i + 1].Time - _keys[i].Time);
				return _keys[i].Value + (t * (_keys[i + 1].Value - _keys[i].Value));
			}
		}

		// Fallback.
		return 1.0f;
	}
}
