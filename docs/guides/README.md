# Guides

Task-focused walkthroughs, each built on the setup below. New to Forge? Start with the [Quick Start](../quick-start.md) first. The examples are exercised by [`GuidesTests`](../../Forge.Tests/Samples/GuidesTests.cs) in the test suite.

| Guide | Covers |
| --- | --- |
| [Effect Recipes](effect-recipes.md) | Instant, timed, infinite, periodic, stacking and unique effects; adding tags with effects; blocking effects with tags. |
| [Extending Effects](extending-effects.md) | Custom components, composing components, custom calculators and custom executions. |
| [Cues and Events](cues-and-events.md) | Registering and triggering cues, writing a cue handler, and raising and subscribing to events. |
| [Ability Recipes](ability-recipes.md) | Defining, granting, removing and activating abilities, and triggering them from events and tags. |
| [Statescript in Code](statescript-in-code.md) | Building a graph in C# and running it as an ability. |

## The Shared Setup

A `Player` entity with four attributes: Health, Mana, Strength and Speed.

For that we need to first define an `AttributeSet` that will hold those attributes.

```csharp
// First we need to create the player attribute set
public class PlayerAttributeSet : AttributeSet
{
    public EntityAttribute Health { get; }
    public EntityAttribute Mana { get; }
    public EntityAttribute Strength { get; }
    public EntityAttribute Speed { get; }

    public PlayerAttributeSet()
    {
        // Initialize the attributes with the current, min and max values.
        Health = InitializeAttribute(nameof(Health), 100, 0, 150);
        Mana = InitializeAttribute(nameof(Mana), 100, 0, 100);
        Strength = InitializeAttribute(nameof(Strength), 10, 0, 99);
        Speed = InitializeAttribute(nameof(Speed), 5, 0, 10);
    }
}

// Then create a new entity that implements IForgeEntity
public class Player : IForgeEntity
{
    public EntityAttributes Attributes { get; }
    public EntityTags Tags { get; }
    public EffectsManager EffectsManager { get; }
    public CuesManager CuesManager { get; }
    public EntityAbilities Abilities { get; }
    public EventManager Events { get; }
    public Variables SharedVariables { get; } = new();

    public Player(TagsManager tagsManager, CuesManager cuesManager)
    {
        // Initialize base tags during construction
        var baseTags = new TagContainer(
            tagsManager,
            new HashSet<Tag>()
            {
                Tag.RequestTag(tagsManager, "character.player"),
                Tag.RequestTag(tagsManager, "class.warrior")
            });

        Attributes = new EntityAttributes(this, new PlayerAttributeSet());
        Tags = new EntityTags(baseTags);
        EffectsManager = new EffectsManager(this, cuesManager);
        CuesManager = cuesManager;
        Abilities = new(this);
        Events = new();
    }
}

// Initialize managers
var cuesManager = new CuesManager();
var tagsManager = new TagsManager(new string[]
{
    "character.player",
    "class.warrior",
    "status.stunned",
    "status.burning",
    "status.enraged",
    "status.immune.fire",
    "cues.damage.fire",
    "events.combat.damage",
    "events.combat.hit",
    "cooldown.fireball",
    "data.damage.dealt"
});

// Create player instance
var player = new Player(tagsManager, cuesManager);

// Access the player's attribute values
var health = player.Attributes["PlayerAttributeSet.Health"].CurrentValue; // 100
var mana = player.Attributes["PlayerAttributeSet.Mana"].CurrentValue; // 100
var strength = player.Attributes["PlayerAttributeSet.Strength"].CurrentValue; // 10
var speed = player.Attributes["PlayerAttributeSet.Speed"].CurrentValue; // 5
```

### Working with Tags

Tags allow you to classify entities and define conditions for effects. Base tags are immutable and assignable only at creation time.

You should generally do checks against the `AllTags` property since it includes both the `BaseTags` and `ModifierTags`.

```csharp
// Tags must be requested through the static method Tag.RequestTag
var playerTag = Tag.RequestTag(tagsManager, "character.player");
var warriorTag = Tag.RequestTag(tagsManager, "class.warrior");

// Check if the player has specific tags
bool isPlayer = player.Tags.AllTags.HasTag(playerTag);
bool isWarrior = player.Tags.AllTags.HasTag(warriorTag);
```

