// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the conjugate of a quaternion operand, using <see cref="Quaternion.Conjugate(Quaternion)"/>.
/// </summary>
/// <param name="operand">The resolver for the quaternion operand.</param>
public class ConjugateResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateType(operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Quaternion value = _operand.Resolve(graphContext).AsQuaternion();
		return new Variant128(Quaternion.Conjugate(value));
	}

	private static Type ValidateType(Type operandType)
	{
		if (operandType != typeof(Quaternion))
		{
			throw new ArgumentException(
				$"ConjugateResolver only supports Quaternion operands. Got '{operandType}'.");
		}

		return typeof(Quaternion);
	}
}
