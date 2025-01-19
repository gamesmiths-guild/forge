// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Attribute = Gamesmiths.Forge.Core.Attribute;

namespace Gamesmiths.Forge.Tests.Helpers;

public class TestAttributeSet : AttributeSet
{
	public Attribute Attribute1 { get; }

	public Attribute Attribute2 { get; }

	public Attribute Attribute3 { get; }

	public Attribute Attribute5 { get; }

	public Attribute Attribute90 { get; }

	public TestAttributeSet()
	{
		Attribute1 = InitializeAttribute(nameof(Attribute1), 1, 0, 99, 2);
		Attribute2 = InitializeAttribute(nameof(Attribute2), 2, 0, 99, 2);
		Attribute3 = InitializeAttribute(nameof(Attribute3), 3, 0, 99, 2);
		Attribute5 = InitializeAttribute(nameof(Attribute5), 5, 0, 99, 2);
		Attribute90 = InitializeAttribute(nameof(Attribute90), 90, 0, 99, 2);
	}
}
