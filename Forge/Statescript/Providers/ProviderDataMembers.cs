// Copyright © Gamesmiths Guild.

using System.Reflection;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Reflection helpers shared by the providers that expose a data type's members to the graph editor. Providers whose
/// read side is a plain projection of their data type get their declared members for free from here, so a provider only
/// declares them by hand when it needs to restrict or reshape the set.
/// </summary>
internal static class ProviderDataMembers
{
	/// <summary>
	/// A public readable member discovered on a provider's data type.
	/// </summary>
	/// <param name="Name">The member name, used as the declared output name.</param>
	/// <param name="ValueType">The member's declared type.</param>
	internal readonly record struct Member(string Name, Type ValueType);

	private readonly record struct OrderedMember(Member Member, int Order);

	/// <summary>
	/// Describes the public readable instance members (fields and get-able, non-indexer properties) of
	/// <paramref name="dataType"/>, in declaration order.
	/// </summary>
	/// <param name="dataType">The data type to describe.</param>
	/// <returns>The discovered members.</returns>
	public static Member[] Describe(Type dataType)
	{
		var members = new List<OrderedMember>();

		foreach (PropertyInfo property in dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
		{
			// Indexers are not addressable by name, so they cannot be declared as outputs.
			if (!property.CanRead || property.GetIndexParameters().Length > 0)
			{
				continue;
			}

			members.Add(new OrderedMember(
				new Member(property.Name, property.PropertyType),
				property.MetadataToken));
		}

		foreach (FieldInfo field in dataType.GetFields(BindingFlags.Instance | BindingFlags.Public))
		{
			// Compiler-generated members are not part of the data's shape; an enum's backing `value__` field is public
			// and would otherwise show up as a bindable member named after an implementation detail.
			if (field.IsSpecialName)
			{
				continue;
			}

			members.Add(new OrderedMember(new Member(field.Name, field.FieldType), field.MetadataToken));
		}

		// Reflection does not guarantee an order; metadata tokens are assigned in declaration order, so sorting by them
		// keeps editor dropdowns stable and matching the source.
		members.Sort(static (left, right) => left.Order.CompareTo(right.Order));

		return [.. members.Select(static entry => entry.Member)];
	}

	/// <summary>
	/// Gets a value indicating whether values of the given type can be stored in a <see cref="Variant128"/>, and so
	/// written to a scalar graph variable.
	/// </summary>
	/// <param name="valueType">The value type to test.</param>
	/// <returns><see langword="true"/> when <see cref="Variant128"/> has a constructor for the type.</returns>
	public static bool IsVariantSupported(Type valueType)
	{
		return typeof(Variant128).GetConstructor([valueType]) is not null;
	}
}
