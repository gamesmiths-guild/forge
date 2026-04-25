// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a quaternion created from yaw, pitch, and roll angles using
/// <see cref="Quaternion.CreateFromYawPitchRoll(float, float, float)"/>.
/// </summary>
/// <param name="yaw">The resolver for the yaw angle in radians.</param>
/// <param name="pitch">The resolver for the pitch angle in radians.</param>
/// <param name="roll">The resolver for the roll angle in radians.</param>
public class QuaternionFromYawPitchRollResolver(IPropertyResolver yaw, IPropertyResolver pitch, IPropertyResolver roll)
	: IPropertyResolver
{
	private readonly IPropertyResolver _yaw = yaw;

	private readonly IPropertyResolver _pitch = pitch;

	private readonly IPropertyResolver _roll = roll;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(yaw.ValueType, pitch.ValueType, roll.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		var yawValue = _yaw.Resolve(graphContext).AsFloat();
		var pitchValue = _pitch.Resolve(graphContext).AsFloat();
		var rollValue = _roll.Resolve(graphContext).AsFloat();
		return new Variant128(Quaternion.CreateFromYawPitchRoll(yawValue, pitchValue, rollValue));
	}

	private static Type ValidateTypes(Type yawType, Type pitchType, Type rollType)
	{
		if (yawType != typeof(float) || pitchType != typeof(float) || rollType != typeof(float))
		{
			throw new ArgumentException(
				"QuaternionFromYawPitchRollResolver requires yaw, pitch, and roll to all be float." +
				$"Got '{yawType}', '{pitchType}', and '{rollType}'.");
		}

		return typeof(Quaternion);
	}
}
