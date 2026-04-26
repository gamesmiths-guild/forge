// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

internal static class BooleanTypeUtils
{
	internal static IPropertyResolver ValidateBoolOperand(
		string resolverName,
		string parameterName,
		IPropertyResolver operand)
	{
		if (operand.ValueType != typeof(bool))
		{
			throw new ArgumentException(
				$"{resolverName} requires {parameterName} to resolve to bool. Got '{operand.ValueType}'.",
				parameterName);
		}

		return operand;
	}
}
