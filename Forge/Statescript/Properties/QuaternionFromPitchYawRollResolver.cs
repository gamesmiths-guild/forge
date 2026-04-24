// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a quaternion created from pitch, yaw, and roll angles, using
/// <see cref="Quaternion.CreateFromYawPitchRoll(float, float, float)"/>.
/// </summary>
/// <param name="pitch">The resolver for the pitch angle in radians.</param>
/// <param name="yaw">The resolver for the yaw angle in radians.</param>
/// <param name="roll">The resolver for the roll angle in radians.</param>
public class QuaternionFromPitchYawRollResolver(IPropertyResolver pitch, IPropertyResolver yaw, IPropertyResolver roll)
	: IPropertyResolver
{
	private readonly IPropertyResolver _pitch = pitch;

	private readonly IPropertyResolver _yaw = yaw;

	private readonly IPropertyResolver _roll = roll;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(pitch.ValueType, yaw.ValueType, roll.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		var pitchValue = _pitch.Resolve(graphContext).AsFloat();
		var yawValue = _yaw.Resolve(graphContext).AsFloat();
		var rollValue = _roll.Resolve(graphContext).AsFloat();
		return new Variant128(Quaternion.CreateFromYawPitchRoll(yawValue, pitchValue, rollValue));
	}

	private static Type ValidateTypes(Type pitchType, Type yawType, Type rollType)
	{
		if (pitchType != typeof(float) || yawType != typeof(float) || rollType != typeof(float))
		{
			throw new ArgumentException(
				"QuaternionFromPitchYawRollResolver requires pitch, yaw, and roll to all be float." +
				$"Got '{pitchType}', '{yawType}', and '{rollType}'.");
		}

		return typeof(Quaternion);
	}
}
