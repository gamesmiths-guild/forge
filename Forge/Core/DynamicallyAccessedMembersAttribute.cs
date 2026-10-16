// Copyright © Gamesmiths Guild.

#if !NET5_0_OR_GREATER
#pragma warning disable IDE0130 // Namespace does not match folder structure
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

// netstandard2.1 lacks this attribute. Trimmers match it by name, so declaring it lets the shared code carry the
// annotation on every target.
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(
	AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
	AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
	AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
	Inherited = false)]
internal sealed class DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) : Attribute
{
	public DynamicallyAccessedMemberTypes MemberTypes { get; } = memberTypes;
}

[Flags]
internal enum DynamicallyAccessedMemberTypes
{
	None = 0,
	PublicFields = 1 << 5,
	PublicProperties = 1 << 9,
}
#endif
