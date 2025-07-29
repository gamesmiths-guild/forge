// Copyright © Gamesmiths Guild.

#pragma warning disable

using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Tags;
using static Gamesmiths.Forge.Tests.Samples.QuickStartTests;

namespace Gamesmiths.Forge.Tests.Samples;

public class ExamplesTestFixture
{
	public TagsManager TagsManager { get; }

	public CuesManager CuesManager { get; }

	public ExamplesTestFixture()
	{
		CuesManager = new CuesManager();
		TagsManager = new TagsManager(new string[]
		{
			"character.player",
			"class.warrior",
			"status.stunned",
			"status.burning",
			"status.immune.fire",
			"cues.damage.fire"
		});

		CuesManager.RegisterCue(
			Tag.RequestTag(TagsManager, "cues.damage.fire"),
			new FireDamageCueHandler()
		);
	}
}
