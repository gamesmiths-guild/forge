// Copyright © Gamesmiths Guild.

using System.Linq.Expressions;
using System.Reflection;
using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value directly from the typed activation data stored in <see cref="AbilityBehaviorContext{TData}.Data"/>.
/// </summary>
/// <remarks>
/// This resolver requires the graph to be driven by <see cref="GraphAbilityBehavior{TData}"/> with a matching
/// activation-data type. It supports reading public instance fields and public readable properties whose types are
/// supported by <see cref="Variant128"/>.
/// </remarks>
public class AbilityActivationDataResolver : IPropertyResolver
{
	private readonly MemberInfo _member;

	private readonly ConstructorInfo _variantConstructor;

	// Compiled from the context's own type on first resolve: an AOT build has that type, but not necessarily one built
	// with MakeGenericType.
	private Func<object, Variant128>? _resolveActivationData;

	/// <summary>
	/// Gets the activation-data type expected in the current ability context.
	/// </summary>
	public Type ActivationDataType { get; }

	/// <summary>
	/// Gets the activation-data member name this resolver reads.
	/// </summary>
	public string MemberName { get; }

	/// <inheritdoc/>
	public Type ValueType { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AbilityActivationDataResolver"/> class.
	/// </summary>
	/// <param name="activationDataType">The runtime activation-data type carried by
	/// <see cref="AbilityBehaviorContext{TData}"/>.</param>
	/// <param name="memberName">The public field or property name to read from the activation data.</param>
	public AbilityActivationDataResolver(Type activationDataType, string memberName)
	{
		EnsureActivationDataType(activationDataType);

		ActivationDataType = activationDataType;
		MemberName = ValidateMemberName(memberName);
		_member = FindMember(activationDataType, MemberName);
		ValueType = GetMemberType(_member);
		_variantConstructor = GetVariantConstructor(ValueType);
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (graphContext.ActivationContext is not AbilityBehaviorContext activationContext
			|| activationContext.DataType != ActivationDataType)
		{
			return default;
		}

		_resolveActivationData ??= CreateResolveDelegate(activationContext.GetType(), _member, _variantConstructor);
		return _resolveActivationData(activationContext);
	}

	private static Func<object, Variant128> CreateResolveDelegate(
		Type activationContextType,
		MemberInfo member,
		ConstructorInfo constructor)
	{
		ParameterExpression activationContextParameter = Expression.Parameter(typeof(object), "activationContext");
		UnaryExpression typedContext = Expression.Convert(activationContextParameter, activationContextType);
		MemberExpression dataAccess = Expression.Property(typedContext, nameof(AbilityBehaviorContext<int>.Data));
		Expression memberAccess = member switch
		{
			PropertyInfo property => Expression.Property(dataAccess, property),
			FieldInfo field => Expression.Field(dataAccess, field),
			_ => throw new InvalidOperationException(
				$"Unsupported activation-data member kind '{member.MemberType}' for '{member.Name}'."),
		};

		NewExpression variantCreation = Expression.New(constructor, memberAccess);
		return Expression.Lambda<Func<object, Variant128>>(variantCreation, activationContextParameter).Compile();
	}

	private static void EnsureActivationDataType(Type activationDataType)
	{
#if NET8_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(activationDataType);
#else
		if (activationDataType is null)
		{
			throw new ArgumentNullException(nameof(activationDataType));
		}
#endif
	}

	private static MemberInfo FindMember(Type activationDataType, string memberName)
	{
		memberName = ValidateMemberName(memberName);

		PropertyInfo? property = activationDataType.GetProperty(
			memberName,
			BindingFlags.Instance | BindingFlags.Public);
		if (property is not null)
		{
			if (!property.CanRead)
			{
				throw new ArgumentException(
					$"AbilityActivationDataResolver requires member '{memberName}' on '{activationDataType}' to be " +
					"readable.",
					nameof(memberName));
			}

			return property;
		}

		FieldInfo? field = activationDataType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
		if (field is not null)
		{
			return field;
		}

		throw new ArgumentException(
			$"AbilityActivationDataResolver could not find public instance field or property '{memberName}' on " +
			$"activation-data type '{activationDataType}'.",
			nameof(memberName));
	}

	private static Type GetMemberType(MemberInfo member)
	{
		return member switch
		{
			PropertyInfo property => property.PropertyType,
			FieldInfo field => field.FieldType,
			_ => throw new InvalidOperationException(
				$"Unsupported activation-data member kind '{member.MemberType}' for '{member.Name}'."),
		};
	}

	private static ConstructorInfo GetVariantConstructor(Type valueType)
	{
		ConstructorInfo? constructor = typeof(Variant128).GetConstructor([valueType]);
		if (constructor is not null)
		{
			return constructor;
		}

		throw new ArgumentException(
			$"AbilityActivationDataResolver does not support member type '{valueType}'. " +
			"Use a graph-variable data binder or a custom resolver for unsupported types.",
			nameof(valueType));
	}

	private static string ValidateMemberName(string memberName)
	{
		if (string.IsNullOrWhiteSpace(memberName))
		{
			throw new ArgumentException(
				"AbilityActivationDataResolver requires a non-empty activation-data member name.",
				nameof(memberName));
		}

		return memberName;
	}
}
