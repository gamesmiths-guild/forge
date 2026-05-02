// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

internal static class GameplayMathUtils
{
	internal static float ClampToUnit(float value)
	{
		return Math.Clamp(value, -1.0f, 1.0f);
	}

	internal static Vector2 Project(Vector2 value, Vector2 onto)
	{
		var denominator = Vector2.Dot(onto, onto);
		if (denominator <= float.Epsilon)
		{
			return Vector2.Zero;
		}

		return onto * (Vector2.Dot(value, onto) / denominator);
	}

	internal static Vector3 Project(Vector3 value, Vector3 onto)
	{
		var denominator = Vector3.Dot(onto, onto);
		if (denominator <= float.Epsilon)
		{
			return Vector3.Zero;
		}

		return onto * (Vector3.Dot(value, onto) / denominator);
	}

	internal static Vector4 Project(Vector4 value, Vector4 onto)
	{
		var denominator = Vector4.Dot(onto, onto);
		if (denominator <= float.Epsilon)
		{
			return Vector4.Zero;
		}

		return onto * (Vector4.Dot(value, onto) / denominator);
	}

	internal static float Angle(Vector2 from, Vector2 to)
	{
		var denominator = from.Length() * to.Length();
		if (denominator <= float.Epsilon)
		{
			return 0.0f;
		}

		return MathF.Acos(ClampToUnit(Vector2.Dot(from, to) / denominator));
	}

	internal static float Angle(Vector3 from, Vector3 to)
	{
		var denominator = from.Length() * to.Length();
		if (denominator <= float.Epsilon)
		{
			return 0.0f;
		}

		return MathF.Acos(ClampToUnit(Vector3.Dot(from, to) / denominator));
	}

	internal static float QuaternionAngle(Quaternion from, Quaternion to)
	{
		var normalizedFrom = Quaternion.Normalize(from);
		var normalizedTo = Quaternion.Normalize(to);
		var dot = MathF.Abs(Quaternion.Dot(normalizedFrom, normalizedTo));
		return 2.0f * MathF.Acos(ClampToUnit(dot));
	}

	internal static float SignedAngle(Vector2 from, Vector2 to)
	{
		var cross = (from.X * to.Y) - (from.Y * to.X);
		var dot = Vector2.Dot(from, to);
		return MathF.Atan2(cross, dot);
	}

	internal static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
	{
		var cross = Vector3.Cross(from, to);
		return MathF.Atan2(Vector3.Dot(axis, cross), Vector3.Dot(from, to));
	}

	internal static float MoveTowards(float current, float target, float maxDelta)
	{
		if (maxDelta <= 0.0f)
		{
			return current;
		}

		var delta = target - current;
		if (MathF.Abs(delta) <= maxDelta)
		{
			return target;
		}

		return current + (MathF.Sign(delta) * maxDelta);
	}

	internal static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
	{
		if (maxDelta <= 0.0f)
		{
			return current;
		}

		Vector2 delta = target - current;
		var distance = delta.Length();
		if (distance <= maxDelta || distance <= float.Epsilon)
		{
			return target;
		}

		return current + (delta / distance * maxDelta);
	}

	internal static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDelta)
	{
		if (maxDelta <= 0.0f)
		{
			return current;
		}

		Vector3 delta = target - current;
		var distance = delta.Length();
		if (distance <= maxDelta || distance <= float.Epsilon)
		{
			return target;
		}

		return current + ((delta / distance) * maxDelta);
	}

	internal static Vector4 MoveTowards(Vector4 current, Vector4 target, float maxDelta)
	{
		if (maxDelta <= 0.0f)
		{
			return current;
		}

		Vector4 delta = target - current;
		var distance = delta.Length();
		if (distance <= maxDelta || distance <= float.Epsilon)
		{
			return target;
		}

		return current + ((delta / distance) * maxDelta);
	}

	internal static Vector2 ClampMagnitude(Vector2 value, float maxLength)
	{
		if (maxLength <= 0.0f)
		{
			return Vector2.Zero;
		}

		var length = value.Length();
		if (length <= maxLength || length <= float.Epsilon)
		{
			return value;
		}

		return (value / length) * maxLength;
	}

	internal static Vector3 ClampMagnitude(Vector3 value, float maxLength)
	{
		if (maxLength <= 0.0f)
		{
			return Vector3.Zero;
		}

		var length = value.Length();
		if (length <= maxLength || length <= float.Epsilon)
		{
			return value;
		}

		return (value / length) * maxLength;
	}

	internal static Vector4 ClampMagnitude(Vector4 value, float maxLength)
	{
		if (maxLength <= 0.0f)
		{
			return Vector4.Zero;
		}

		var length = value.Length();
		if (length <= maxLength || length <= float.Epsilon)
		{
			return value;
		}

		return (value / length) * maxLength;
	}

	internal static Quaternion RotateTowards(Quaternion current, Quaternion target, float maxRadiansDelta)
	{
		if (maxRadiansDelta <= 0.0f)
		{
			return current;
		}

		var normalizedCurrent = Quaternion.Normalize(current);
		var normalizedTarget = Quaternion.Normalize(target);
		if (Quaternion.Dot(normalizedCurrent, normalizedTarget) < 0.0f)
		{
			normalizedTarget = -normalizedTarget;
		}

		var angle = QuaternionAngle(normalizedCurrent, normalizedTarget);
		if (angle <= maxRadiansDelta || angle <= float.Epsilon)
		{
			return normalizedTarget;
		}

		return Quaternion.Normalize(Quaternion.Slerp(normalizedCurrent, normalizedTarget, maxRadiansDelta / angle));
	}

	internal static Quaternion LookAt(Vector3 from, Vector3 to, Vector3 up)
	{
		Vector3 forward = to - from;
		if (forward.LengthSquared() <= float.Epsilon)
		{
			return Quaternion.Identity;
		}

		forward = Vector3.Normalize(forward);
		var right = Vector3.Cross(up, forward);
		if (right.LengthSquared() <= float.Epsilon)
		{
			right = Vector3.Cross(Vector3.UnitY, forward);
			if (right.LengthSquared() <= float.Epsilon)
			{
				right = Vector3.Cross(Vector3.UnitX, forward);
			}
		}

		right = Vector3.Normalize(right);
		var correctedUp = Vector3.Normalize(Vector3.Cross(forward, right));

		var matrix = new Matrix4x4(
			right.X,
			right.Y,
			right.Z,
			0.0f,
			correctedUp.X,
			correctedUp.Y,
			correctedUp.Z,
			0.0f,
			forward.X,
			forward.Y,
			forward.Z,
			0.0f,
			0.0f,
			0.0f,
			0.0f,
			1.0f);

		return Quaternion.CreateFromRotationMatrix(matrix);
	}

	internal static Vector2 RandomInsideCircle(Core.IRandom random)
	{
		var angle = random.NextSingle() * (2.0f * MathF.PI);
		var radius = MathF.Sqrt(random.NextSingleInclusive());
		return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
	}

	internal static Vector2 RandomDirection(Core.IRandom random)
	{
		var angle = random.NextSingle() * (2.0f * MathF.PI);
		return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
	}

	internal static Vector3 RandomOnSphere(Core.IRandom random)
	{
		var z = 1.0f - (2.0f * random.NextSingleInclusive());
		var angle = random.NextSingle() * (2.0f * MathF.PI);
		var radius = MathF.Sqrt(MathF.Max(0.0f, 1.0f - (z * z)));
		return new Vector3(radius * MathF.Cos(angle), radius * MathF.Sin(angle), z);
	}

	internal static Vector3 RandomInsideSphere(Core.IRandom random)
	{
		Vector3 direction = RandomOnSphere(random);
		var radius = MathF.Pow(random.NextSingleInclusive(), 1.0f / 3.0f);
		return direction * radius;
	}

	internal static Quaternion QuaternionFromEulerAngles(
		Vector3 eulerAngles,
		EulerOrder order = EulerOrder.XYZ)
	{
		var pitch = Quaternion.CreateFromAxisAngle(Vector3.UnitX, eulerAngles.X);
		var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, eulerAngles.Y);
		var roll = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, eulerAngles.Z);

		Quaternion quaternion = order switch
		{
			EulerOrder.XYZ => pitch * yaw * roll,
			EulerOrder.XZY => pitch * roll * yaw,
			EulerOrder.YXZ => yaw * pitch * roll,
			EulerOrder.YZX => yaw * roll * pitch,
			EulerOrder.ZXY => roll * pitch * yaw,
			EulerOrder.ZYX => roll * yaw * pitch,
			_ => throw new ArgumentOutOfRangeException(nameof(order), order, "Unsupported Euler order."),
		};

		return Quaternion.Normalize(quaternion);
	}

	internal static Vector3 QuaternionToEulerAngles(
		Quaternion quaternion,
		EulerOrder order = EulerOrder.XYZ)
	{
		var normalized = Quaternion.Normalize(quaternion);
		var matrix = Matrix4x4.CreateFromQuaternion(normalized);
		const float singularityThreshold = 0.9999999f;

		float pitch;
		float yaw;
		float roll;

		switch (order)
		{
			case EulerOrder.XYZ:
				yaw = MathF.Asin(ClampToUnit(matrix.M31));
				if (MathF.Abs(matrix.M31) < singularityThreshold)
				{
					pitch = MathF.Atan2(-matrix.M32, matrix.M33);
					roll = MathF.Atan2(-matrix.M21, matrix.M11);
				}
				else
				{
					pitch = MathF.Atan2(matrix.M23, matrix.M22);
					roll = 0.0f;
				}

				break;

			case EulerOrder.XZY:
				roll = MathF.Asin(-ClampToUnit(matrix.M21));
				if (MathF.Abs(matrix.M21) < singularityThreshold)
				{
					pitch = MathF.Atan2(matrix.M23, matrix.M22);
					yaw = MathF.Atan2(matrix.M31, matrix.M11);
				}
				else
				{
					pitch = MathF.Atan2(-matrix.M32, matrix.M33);
					yaw = 0.0f;
				}

				break;

			case EulerOrder.YXZ:
				pitch = MathF.Asin(-ClampToUnit(matrix.M32));
				if (MathF.Abs(matrix.M32) < singularityThreshold)
				{
					yaw = MathF.Atan2(matrix.M31, matrix.M33);
					roll = MathF.Atan2(matrix.M12, matrix.M22);
				}
				else
				{
					yaw = MathF.Atan2(-matrix.M13, matrix.M11);
					roll = 0.0f;
				}

				break;

			case EulerOrder.YZX:
				roll = MathF.Asin(ClampToUnit(matrix.M12));
				if (MathF.Abs(matrix.M12) < singularityThreshold)
				{
					pitch = MathF.Atan2(-matrix.M32, matrix.M22);
					yaw = MathF.Atan2(-matrix.M13, matrix.M11);
				}
				else
				{
					pitch = 0.0f;
					yaw = MathF.Atan2(matrix.M31, matrix.M33);
				}

				break;

			case EulerOrder.ZXY:
				pitch = MathF.Asin(ClampToUnit(matrix.M23));
				if (MathF.Abs(matrix.M23) < singularityThreshold)
				{
					yaw = MathF.Atan2(-matrix.M13, matrix.M33);
					roll = MathF.Atan2(-matrix.M21, matrix.M22);
				}
				else
				{
					yaw = 0.0f;
					roll = MathF.Atan2(matrix.M12, matrix.M11);
				}

				break;

			case EulerOrder.ZYX:
				yaw = MathF.Asin(-ClampToUnit(matrix.M13));
				if (MathF.Abs(matrix.M13) < singularityThreshold)
				{
					pitch = MathF.Atan2(matrix.M23, matrix.M33);
					roll = MathF.Atan2(matrix.M12, matrix.M11);
				}
				else
				{
					pitch = 0.0f;
					roll = MathF.Atan2(-matrix.M21, matrix.M22);
				}

				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(order), order, "Unsupported Euler order.");
		}

		return new Vector3(pitch, yaw, roll);
	}
}
