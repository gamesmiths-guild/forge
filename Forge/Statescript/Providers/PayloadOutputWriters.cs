// Copyright © Gamesmiths Guild.

using System.Linq.Expressions;
using System.Reflection;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Builds the compiled delegates that write a payload's members to their bound graph variables, backing the default
/// <see cref="EventPayloadProvider{TPayload}.WriteOutputs"/>. Each member is compiled once per payload type into a
/// direct call to the matching <see cref="EventPayloadOutputs"/> overload, so the default costs no reflection per
/// event.
/// </summary>
internal static class PayloadOutputWriters
{
	/// <summary>
	/// Builds a writer for every member of <typeparamref name="TPayload"/> that can be written to a graph variable,
	/// paired with the output that declares it. Members whose type is neither <see cref="Variant128"/>-backed nor a
	/// reference type are skipped: nothing could write them, so declaring them would offer the editor a binding that
	/// never updates.
	/// </summary>
	/// <typeparam name="TPayload">The payload type to describe.</typeparam>
	/// <param name="outputs">The declared outputs, parallel to the returned writers.</param>
	/// <returns>The compiled writers, parallel to <paramref name="outputs"/>.</returns>
	public static Action<TPayload, EventPayloadOutputs>[] Build<TPayload>(out EventPayloadOutput[] outputs)
	{
		var declared = new List<EventPayloadOutput>();
		var writers = new List<Action<TPayload, EventPayloadOutputs>>();

		foreach (ProviderDataMembers.Member member in ProviderDataMembers.Describe(typeof(TPayload)))
		{
			Action<TPayload, EventPayloadOutputs>? writer = TryBuildWriter<TPayload>(member);

			if (writer is null)
			{
				continue;
			}

			declared.Add(new EventPayloadOutput(member.Name, member.ValueType));
			writers.Add(writer);
		}

		outputs = [.. declared];
		return [.. writers];
	}

	private static Action<TPayload, EventPayloadOutputs>? TryBuildWriter<TPayload>(ProviderDataMembers.Member member)
	{
		MethodInfo? setter = FindSetter(member.ValueType);

		if (setter is null)
		{
			return null;
		}

		ParameterExpression payload = Expression.Parameter(typeof(TPayload), "payload");
		ParameterExpression outputs = Expression.Parameter(typeof(EventPayloadOutputs), "outputs");

		MethodCallExpression call = Expression.Call(
			outputs,
			setter,
			Expression.Constant(member.Name),
			Expression.PropertyOrField(payload, member.Name));

		return Expression.Lambda<Action<TPayload, EventPayloadOutputs>>(call, payload, outputs).Compile();
	}

	private static MethodInfo? FindSetter(Type valueType)
	{
		// Single-precision floats have a dedicated overload that widens without boxing; prefer it over the generic one.
		if (valueType == typeof(float))
		{
			return typeof(EventPayloadOutputs).GetMethod(
				nameof(EventPayloadOutputs.Set),
				[typeof(string), typeof(float)]);
		}

		if (ProviderDataMembers.IsVariantSupported(valueType))
		{
			return GetGenericMethod(nameof(EventPayloadOutputs.Set))?.MakeGenericMethod(valueType);
		}

		// Reference types travel the object lane instead.
		return valueType.IsValueType
			? null
			: GetGenericMethod(nameof(EventPayloadOutputs.SetObject))?.MakeGenericMethod(valueType);
	}

	private static MethodInfo? GetGenericMethod(string name)
	{
		return Array.Find(
			typeof(EventPayloadOutputs).GetMethods(BindingFlags.Instance | BindingFlags.Public),
			method => method.Name == name && method.IsGenericMethodDefinition);
	}
}
