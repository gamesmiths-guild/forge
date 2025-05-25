// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Tests.Helpers;

public class TestAttributeSet : AttributeSet
{
	public EntityAttribute Attribute1 { get; }

	public EntityAttribute Attribute2 { get; }

	public EntityAttribute Attribute3 { get; }

	public EntityAttribute Attribute5 { get; }

	public EntityAttribute Attribute90 { get; }

	public TestAttributeSet()
	{
		Attribute1 = InitializeAttribute(nameof(Attribute1), 1, 0, 99, 2);
		Attribute2 = InitializeAttribute(nameof(Attribute2), 2, 0, 99, 2);
		Attribute3 = InitializeAttribute(nameof(Attribute3), 3, 0, 99, 2);
		Attribute5 = InitializeAttribute(nameof(Attribute5), 5, 0, 99, 2);
		Attribute90 = InitializeAttribute(nameof(Attribute90), 90, 0, 99, 2);
	}
}
