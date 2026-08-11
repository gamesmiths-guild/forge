# Attributes System

The Attributes system in Forge provides a robust framework for managing numeric properties of game entities. Attributes represent any quantifiable characteristic like health, strength, movement speed, or any other property that can be represented numerically.

For a practical guide on using attributes, see the [Quick Start Guide](quick-start.md).

## Core Concepts

### EntityAttribute

An `EntityAttribute` represents a single numeric property with constraints and modification tracking:

- **Key**: Identifier in the format `<AttributeSet>.<AttributeName>` (e.g. `CombatAttributeSet.MaxHealth`).
- **BaseValue**: The fundamental value before modifications.
- **CurrentValue**: The actual value after all modifications, constrained by Min/Max.
- **Min/Max**: The lower and upper bounds for the attribute.
- **Modifier**: The cumulative modification applied to the BaseValue.
- **Overflow**: Value that exceeds Min/Max constraints (useful for effects like shield overflow).
- **ValidModifier**: The effective modifier value that isn't causing overflow (Modifier - Overflow).
- **DecimalPlaces / DisplayScale / DisplayValue**: An optional presentation-only scale and the helpers that read it — see [Representing Fractional Values](#representing-fractional-values).

### Attribute Values Are Integers

**Every attribute value in Forge is an `int`.** `BaseValue`, `CurrentValue`, `Min`, `Max`, `Modifier`, `Overflow` and `ValidModifier` are all integers, and so are the values written by `SetAttributeBaseValue`, `AddToAttributeBaseValue`, `SetAttributeMinValue` and `SetAttributeMaxValue`.

This is a deliberate design decision, not a limitation waiting to be lifted: integer arithmetic is exactly reproducible across machines and platforms, which the planned networking model depends on. Floating-point attribute state would make two clients running the same simulation drift apart.

Magnitudes are still authored as floats — `ScalableFloat`, `AttributeBasedFloat`, `SetByCallerFloat` and custom calculators all compute in `float`/`double`. The result is **truncated toward zero** when it lands on the attribute, and the attribute's final value is clamped between `Min` and `Max`.

```csharp
// Authored as a float, stored as an int
var healthAttribute = entity.Attributes["CombatAttributeSet.CurrentHealth"];
// A magnitude of -7.9 removes 7 points, not 8: the fractional part is truncated.
```

#### Representing Fractional Values

When a stat conceptually needs decimals (movement speed of `4.75`, a critical chance of `27.5%`), store it **scaled** and tell the attribute how far the scale goes with `decimalPlaces`:

```csharp
public class MovementAttributeSet : AttributeSet
{
    // Speed is stored in hundredths: 475 means 4.75 units/second.
    public EntityAttribute Speed { get; }

    public MovementAttributeSet()
    {
        Speed = InitializeAttribute(nameof(Speed), 475, 0, 10_000, decimalPlaces: 2);
    }
}

// Presentation code reads the scale off the attribute instead of hard-coding the divisor
EntityAttribute speed = entity.Attributes["MovementAttributeSet.Speed"];

int raw = speed.CurrentValue;       // 475 — what the simulation works with
float display = speed.DisplayValue; // 4.75f
string label = speed.ToDisplayString(speed.CurrentValue, CultureInfo.CurrentCulture); // "4.75"
```

> **`decimalPlaces` is presentation only.** It does not make the attribute fractional. `BaseValue`, `CurrentValue`, `Min`, `Max`, `Modifier` and `Overflow` are still the same `int`s they were, modifiers are still authored and evaluated in raw units, comparisons and clamping still happen on raw integers, and nothing in the effect pipeline reads the setting. All it records is what the stored number is *meant to read as*, so UI, tooltips, cue handlers and logs stop each having to know the convention. Leave it at its default of `0` and everything behaves exactly as before.

Because all modifiers operate on the same raw integers, a `FlatBonus` of `+50` on the example above is a `+0.5` speed bonus — no conversion is needed anywhere inside the simulation, only at the presentation boundary.

##### Display Helpers

| Member | Type | What it is |
|--------|------|------------|
| `DecimalPlaces` | `int` | How many decimals the stored integer stands for. `0` by default; at most `9`, since the scale has to fit in an `int`. |
| `DisplayScale` | `int` | The divisor those places imply — `10^DecimalPlaces`, so `1` when unscaled. |
| `DisplayValue` | `float` | `CurrentValue` in display units. |
| `ToDisplayValue(int raw)` | `float` | Any raw value in display units — `Max`, `Modifier`, `Overflow`, or the `change` an `OnValueChanged` handler is handed. |
| `ToDisplayString(int raw, IFormatProvider)` | `string` | The same, formatted with exactly `DecimalPlaces` decimals, so `400` reads as `"4.00"` rather than `"4"`. The culture is required rather than defaulted: pass `CultureInfo.CurrentCulture` for text a player reads and `CultureInfo.InvariantCulture` for anything that must look the same everywhere. |
| `ToRawValue(float display)` | `int` | The inverse, for **authoring** surfaces — an editor field or tool where someone types `4.75` and the attribute has to store `475`. Rounds halves away from zero and converts units only; it does not clamp to `Min`/`Max`. |

The same four conversions exist as statics on `Quantization` — in `Gamesmiths.Forge.Core`, since packing decimals into an integer is not attribute-specific — taking the decimal places as an argument, for the cases where there is no attribute instance to ask — a cue handler whose `target` came through null, editor tooling, a number read back out of save data:

```csharp
Quantization.GetScale(2);                                           // 100
Quantization.ToDisplayValue(475, 2);                                // 4.75f
Quantization.ToDisplayString(475, 2, CultureInfo.InvariantCulture); // "4.75"
Quantization.ToRawValue(4.75f, 2);                                  // 475
```

**Prefer the `EntityAttribute` members whenever you hold the attribute.** The number passed to the statics is a copy of something the attribute already knows, and a copy goes stale: change `decimalPlaces` on the attribute and every hard-coded call site keeps converting by the old scale, quietly and wrongly. The statics are the escape hatch, not the default.

[Cue](cues.md) magnitudes arrive raw as well — an `AttributeCurrentValue` or `AttributeValueChange` cue hands the handler the stored integer — so a handler that wants the scaled reading converts through the attribute it came from, which it can reach from the `target` it is given. Going through the attribute rather than `Quantization` is what keeps this handler correct if the attribute's scale is ever changed:

```csharp
public void OnExecute(IForgeEntity? target, CueParameters? parameters)
{
    EntityAttribute health = target!.Attributes["CombatAttributeSet.CurrentHealth"];
    ShowFloatingNumber(health.ToDisplayString(parameters!.Value.Magnitude, CultureInfo.CurrentCulture));
}
```

Ratios need no conversion at all, since the scale cancels out: a health bar can keep using `CurrentValue / (float)Max`.

Percent-based modifiers are unaffected by the scale: `PercentBonus` multiplies whatever integer is stored, so `+20%` means the same thing whether `Speed` holds `475` or `4`. Prefer a larger scale for attributes that receive percentage modifiers, since truncation on a small integer loses proportionally more precision.

### AttributeSet

AttributeSets group related attributes together and can establish relationships between them:

```csharp
public class CombatAttributeSet : AttributeSet
{
    public EntityAttribute MaxHealth { get; }
    public EntityAttribute CurrentHealth { get; }
    public EntityAttribute AttackPower { get; }

    public CombatAttributeSet()
    {
        // Initialize attributes with (name, defaultValue, minValue, maxValue)
        MaxHealth = InitializeAttribute(nameof(MaxHealth), 100, 0, 1000);
        CurrentHealth = InitializeAttribute(nameof(CurrentHealth), 100, 0, MaxHealth.CurrentValue);
        AttackPower = InitializeAttribute(nameof(AttackPower), 10, 0, 100);
    }

    // Respond to attribute changes
    protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
    {
        if (attribute == MaxHealth)
        {
            // Update CurrentHealth's maximum when MaxHealth changes
            SetAttributeMaxValue(CurrentHealth, MaxHealth.CurrentValue);
        }
    }
}
```

### EntityAttributes

`EntityAttributes` is a container class that manages all AttributeSets for an entity and provides access to individual attributes:

```csharp
public class PlayerCharacter : IForgeEntity
{
    public EntityAttributes Attributes { get; }
    // Other IForgeEntity properties...

    public PlayerCharacter()
    {
        // Create attribute sets
        var combatStats = new CombatAttributeSet();
        var resourceStats = new ResourceAttributeSet();

        // Initialize EntityAttributes with the attribute sets
        Attributes = new EntityAttributes(this, [combatStats, resourceStats]);
    }
}
```

The container takes the entity that owns it, like `EntityAbilities` and `EffectsManager` do. That is what lets it keep the entity's active effects in step when its [sets change at runtime](#adding-and-removing-attribute-sets); assign it in any order relative to the other managers, since the owner is only used later.

Use `TryGetAttribute` to reach an attribute that may not be present — the indexer throws for an unknown key, and attributes can come and go with their sets:

```csharp
if (entity.Attributes.TryGetAttribute("CombatAttributeSet.CurrentHealth", out EntityAttribute? health))
{
    Console.WriteLine(health.CurrentValue);
}
```

## Attribute Identification

Attributes are identified by their fully qualified name using the pattern: `AttributeSetName.AttributeName`

```csharp
// Example of accessing an attribute through EntityAttributes indexer
var healthAttribute = entity.Attributes["CombatAttributeSet.CurrentHealth"];
var currentHealth = healthAttribute.CurrentValue;
```

**Important**: Although this uses dot notation similar to [Tags](tags.md), these are not tags and do not need to be registered with the `TagsManager`.

## Attribute Channels

Channels provide powerful, layered attribute calculation with clearly defined order of operations. Each attribute has one or more channels, which process [modifiers](effects/modifiers.md) in sequence.

### How Channels Work

1. Each channel processes modifiers in this order:
   - If an override is active on the channel, it **replaces** the incoming value and the channel's flat and percentage modifiers are skipped. Overrides default to last-applied-wins, but can opt into `AggregationMode.Max`/`Min` arbitration (see [Aggregating Overrides](effects/modifiers.md#aggregating-overrides)). 
   - Otherwise, apply flat modifiers (addition/subtraction), then percentage modifiers (multiplication).
   - Modifiers of the same operation are combined according to their [aggregation mode](effects/modifiers.md#modifier-aggregation): summed by default, or reduced to the max/min value of their group.

2. Channels are processed in sequence, where the output of one channel becomes the input of the next:

```
Channel 1:  (BaseValue + FlatMod1) * PercentMod1  →  Result1
Channel 2:  (Result1 + FlatMod2) * PercentMod2    →  Result2
Channel 3:  (Result2 + FlatMod3) * PercentMod3    →  FinalValue
```

3. The result is clamped between `Min` and `Max` to produce `CurrentValue`; anything outside those bounds is reported through `Overflow`.

### Channel Configuration

When initializing an attribute, you can specify the number of channels:

```csharp
// Create attribute with 3 channels for complex calculations
var damage = InitializeAttribute(nameof(Damage), 10, 0, 100, channels: 3);
```

### Channel Use Cases

Channels enable complex formulas like `(x + y) * (z + w)` by separating modifiers into appropriate channels:

- **Channel 0**: Base stats and inherent modifiers.
- **Channel 1**: Equipment and item bonuses.
- **Channel 2**: Temporary buffs and status effects.
- **Channel 3**: Final adjustments like damage reduction.

Example: `(BaseAttack + WeaponDamage) * (1 + StrengthBonus) * (1 + CriticalMultiplier) * (1 - TargetArmor)`

## Working with AttributeSets

### Creating an AttributeSet

To create an AttributeSet, extend the base class and initialize attributes in the constructor using the provided `InitializeAttribute` method:

```csharp
public class ResourceAttributeSet : AttributeSet
{
    public EntityAttribute MaxMana { get; }
    public EntityAttribute CurrentMana { get; }
    public EntityAttribute ManaRegenRate { get; }

    public ResourceAttributeSet()
    {
        // Must use InitializeAttribute to properly register attributes with the system
        MaxMana = InitializeAttribute(nameof(MaxMana), 100, 0, 500);
        CurrentMana = InitializeAttribute(nameof(CurrentMana), 100, 0, MaxMana.CurrentValue);
        ManaRegenRate = InitializeAttribute(nameof(ManaRegenRate), 2, 0, 50);
    }

    protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
    {
        if (attribute == MaxMana)
        {
            // Update CurrentMana's max value
            SetAttributeMaxValue(CurrentMana, MaxMana.CurrentValue);
        }

        if (attribute == CurrentMana && change < 0)
        {
            // Log mana consumption
            Console.WriteLine($"Consumed {-change} mana");
        }
    }
}
```

### AttributeSet Protected Methods

AttributeSet provides several protected methods to manage attributes within the set:

| Method                      | Purpose                                            |
|-----------------------------|----------------------------------------------------|
| **InitializeAttribute**     | Creates and registers a new attribute with the set |
| **SetAttributeBaseValue**   | Sets the base value of an attribute                |
| **AddToAttributeBaseValue** | Adds to the base value of an attribute             |
| **SetAttributeMinValue**    | Sets the minimum value constraint                  |
| **SetAttributeMaxValue**    | Sets the maximum value constraint                  |
| **AttributeOnValueChanged** | Override to handle attribute value changes         |

Example usage:
```csharp
// In an AttributeSet method
SetAttributeBaseValue(Strength, 15);       // Set strength base to 15
AddToAttributeBaseValue(CurrentHealth, -5); // Reduce health by 5
SetAttributeMaxValue(MaxMana, 200);        // Set max mana limit to 200
```

### Attribute Dependencies

AttributeSets allow creating dependencies between attributes without using the [Effects system](effects/README.md):

```csharp
public class CharacterAttributeSet : AttributeSet
{
    public EntityAttribute Strength { get; }
    public EntityAttribute Vitality { get; }
    public EntityAttribute MaxHealth { get; }

    public CharacterAttributeSet()
    {
        Strength = InitializeAttribute(nameof(Strength), 10, 1, 100);
        Vitality = InitializeAttribute(nameof(Vitality), 10, 1, 100);
        MaxHealth = InitializeAttribute(nameof(MaxHealth), 100, 10, 1000);
    }

    protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
    {
        if (attribute == Vitality)
        {
            // Health scales with Vitality
            SetAttributeBaseValue(MaxHealth, Vitality.CurrentValue * 10);
        }
    }
}
```

## Modifying Attributes

There are two primary ways to modify attributes:

### 1. Within AttributeSets

AttributeSets can modify their own attributes using protected methods for direct, permanent changes to the base value:

```csharp
protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
{
    // Add to the base value
    AddToAttributeBaseValue(CurrentHealth, -10);  // Take 10 damage

    // Set the base value directly
    SetAttributeBaseValue(CurrentHealth, 50);     // Set health to 50

    // Modify constraints
    SetAttributeMinValue(Strength, 5);            // Set minimum strength
    SetAttributeMaxValue(MaxHealth, 200);         // Set maximum health
}
```

### 2. Through the Effects System

During gameplay, attributes should be modified exclusively through the [Effects system](effects/README.md), which applies temporary or permanent [modifiers](effects/modifiers.md) to attributes without changing their base value.

```csharp
// Create a damage effect that applies a temporary modifier
var damageEffectData = new EffectData(
    "Damage Effect",
    new DurationData(DurationType.Instant),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.FlatBonus, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-25)))
    }
);

var effect = new Effect(damageEffectData, new EffectOwnership(caster, caster));

// Apply effect to target
target.EffectsManager.ApplyEffect(effect);
```

**Important**: Direct manipulation of attributes outside of these two methods is not supported. All attribute methods besides properties are internal to enforce this pattern.

## Attribute Events

Attributes dispatch an event whenever their `CurrentValue` changes. There are two ways to observe it.

### Inside the AttributeSet

Override `AttributeOnValueChanged`. `InitializeAttribute` subscribes this override to every attribute it creates, so it receives changes for all attributes in the set:

```csharp
// Within an AttributeSet
protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
{
    if (attribute == CurrentHealth)
    {
        if (change < 0)
        {
            // Handle damage
            if (CurrentHealth.CurrentValue <= 0)
            {
                TriggerDeathEvent();
            }
        }
        else if (change > 0)
        {
            // Handle healing
            TriggerHealingEffect(change);
        }
    }
}
```

### From Outside the AttributeSet

`EntityAttribute.OnValueChanged` is a public event, so UI, presentation code and [effect components](effects/components/README.md) can subscribe to a single attribute without owning its set:

```csharp
public event Action<EntityAttribute, int>? OnValueChanged;
```

```csharp
// A health bar that only cares about one attribute
public sealed class HealthBar : IDisposable
{
    private readonly EntityAttribute _health;

    public HealthBar(IForgeEntity entity)
    {
        _health = entity.Attributes["CombatAttributeSet.CurrentHealth"];
        _health.OnValueChanged += HandleHealthChanged;
    }

    private void HandleHealthChanged(EntityAttribute attribute, int change)
    {
        // 'change' is the delta; attribute.CurrentValue is the new value
        Redraw(attribute.CurrentValue, attribute.Max);
    }

    // Always unsubscribe: the attribute outlives the observer
    public void Dispose() => _health.OnValueChanged -= HandleHealthChanged;
}
```

Both paths receive the same notification — `AttributeOnValueChanged` is simply a handler that `InitializeAttribute` attaches to `OnValueChanged` on your behalf.

Two behaviors are worth knowing about:

- **A change that is fully clamped does not fire the event.** If an attribute is already at `Max` and a modifier pushes it further up, `CurrentValue` never moves, so no notification is dispatched. Watch `Overflow` if you need to react to wasted magnitude.
- **Changing `Min` or `Max` can fire the event**, because moving a bound can force `CurrentValue` to move with it.

Notifications are also **batched**. Changes accumulate while an effect is being applied, removed or executed, and are flushed once that operation finishes, so a handler sees the net delta and a consistent attribute state rather than every intermediate step of a multi-modifier effect.

## Advanced Concepts

### Overflow and ValidModifier

When modifiers would push an attribute beyond its Min or Max constraints, the `Overflow` property tracks this excess value:

```
Example: An attribute with:
 - BaseValue = 100
 - Min = 0
 - Max = 150
 - Current applied modifier = +70

The attribute's properties will show:
 - BaseValue = 100 (unchanged)
 - CurrentValue = 150 (clamped at Max)
 - Modifier = +70 (total modification applied)
 - Overflow = +20 (the amount exceeding Max)
 - ValidModifier = +50 (the effective portion of the modifier: 70 - 20)
```

The `ValidModifier` property gives you the portion of the modifier that is actually affecting the attribute's value. This is useful for:

- Calculating partial effectiveness of buffs and debuffs
- Determining when effects are being wasted due to attribute caps
- Creating UI elements that show effective vs. total modifiers
- Triggering game events when modifiers are partially effective

### Multiple Attribute Sets

Entities can have multiple attribute sets for different aspects of gameplay:

```csharp
public class PlayerCharacter : IForgeEntity
{
    public EntityAttributes Attributes { get; }
    // Other IForgeEntity properties...

    public PlayerCharacter()
    {
        // Create different attribute sets
        var combatStats = new CombatAttributeSet();
        var resourceStats = new ResourceAttributeSet();
        var movementStats = new MovementAttributeSet();

        // Initialize entity attributes with all sets
        Attributes = new EntityAttributes(this, [combatStats, resourceStats, movementStats]);
    }

    // Example of accessing an attribute
    public void PrintHealth()
    {
        var health = Attributes["CombatAttributeSet.CurrentHealth"].CurrentValue;
        Console.WriteLine($"Current health: {health}");
    }
}
```

### Adding and Removing Attribute Sets

Sets are not fixed at construction. `AddAttributeSet` and `RemoveAttributeSet` change an entity's attributes at runtime — for transformations, mounts, possession, modular gear that carries its own stats, or recycling a pooled entity:

```csharp
// Werewolf form brings its own stats along
entity.Attributes.AddAttributeSet(werewolfSet);

// ...and takes them away again
bool removed = entity.Attributes.RemoveAttributeSet(werewolfSet);
```

`RemoveAttributeSet` returns `false` when the set is not on the entity. `OnAttributeSetAdded` and `OnAttributeSetRemoved` announce both, after the change has fully settled.

**The set is not modified.** It keeps its attributes and their current values, so removing and re-adding the same instance restores it exactly as it left. Note that keys derive from the set's runtime type name, so an entity cannot hold two instances of the same `AttributeSet` subclass at once.

**Active effects survive the change.** They are not removed, cancelled, or reapplied from scratch. On removal, an effect's modifiers for the departing attributes are unwound and then dropped when it re-evaluates, while its modifiers for attributes the entity keeps go on applying:

```csharp
// One effect, modifiers on two sets
entity.Attributes.RemoveAttributeSet(movementStats);
// -> the MovementAttributeSet.Speed modifier is gone
// -> the CombatAttributeSet.Attack modifier is untouched
// -> the effect is still active
```

This matches how the rest of the system treats a modifier naming an attribute the target does not have: it is skipped, not an error. Adding a set works the same way in reverse — an active effect that carries a modifier for one of the arriving attributes starts contributing immediately, rather than waiting for something else to trigger a re-evaluation.

Three consequences are worth knowing:

- **Requirements re-evaluate.** An [attribute requirement](effects/components/attribute-requirements-effect-component.md) naming a departed attribute is never met, so an effect with an *ongoing* requirement on one becomes inhibited, and un-inhibits when the set comes back.
- **Snapshots are not rolled back.** A value already captured into an effect's snapshot stays as it was read. A snapshot is a reading taken at a point in time, not a live link.
- **Ability costs fail loudly.** An ability whose cost is charged against a departed attribute becomes uncastable, failing with `AbilityActivationFailures.InsufficientResources`. This is the one place where a missing attribute is an error rather than a skip — a cost that can never be paid is refused instead of being quietly ignored, which is what stops the ability from being cast for free.

`AttributeSets` is read-only: the manager keeps it in step with the attribute mapping behind the indexer, so sets are added and removed through these methods rather than through the list.

**Adding a set whose keys collide throws.** An entity cannot hold two instances of the same `AttributeSet` subclass, since keys derive from the set's type name. The collision is detected before anything changes, so a rejected add leaves the entity exactly as it was.

> **Known limit: the rebuild covers only the changed entity's own effects.** An effect living on *another* entity that reads this one's attributes — through a non-snapshot capture resolving to `Source`, or a [source attribute requirement](effects/components/source-attribute-requirements-effect-component.md) watching this entity — is not re-evaluated, and goes on using the values and subscriptions it had. If you change the sets of an entity that other entities' effects capture from, re-apply those effects.

## Integration with Other Systems

While detailed relationships with other systems are covered in their respective documentation, attributes are designed to work seamlessly with them:

- **[Effects](effects/README.md)**: Apply temporary or permanent modifications to attributes.
- **[Tags](tags.md)**: Effects can have tag requirements for attribute modification.
- **[Custom Calculators](effects/calculators.md)**: Complex attribute calculations can be encapsulated in custom calculators.

## Best Practices

1. **Group Related Attributes**: Organize attributes into logical sets.
2. **Use AttributeSet for Relationships**: Handle relationships between attributes in the AttributeSet when possible.
3. **Prefer Effects for Gameplay Changes**: During gameplay, modify attributes through Effects.
4. **Design Channel Strategy**: Plan which modifiers belong in which channels.
5. **Document Attribute Dependencies**: Keep track of which attributes affect others.
6. **Consistent Naming**: Use clear, consistent naming conventions for attributes.
7. **Respect Encapsulation**: Never attempt to directly modify attributes outside of AttributeSets or the Effects system.
8. **Use ValidModifier for UI**: When showing modifier values in UI, consider whether to show the total modifier or the ValidModifier.
9. **Pick a Scale and Declare It**: Attributes are integers. For stats that need decimals, choose a fixed scale (x10, x100, ...), pass it as `decimalPlaces` so presentation code can read it off the attribute, apply it consistently to every effect touching that attribute, and convert only when displaying.
10. **Unsubscribe from `OnValueChanged`**: An observer that subscribes must detach when it goes away — and an attribute can outlive its place on the entity, since [removing its set](#adding-and-removing-attribute-sets) detaches it while leaving the object itself alive. Follow `OnAttributeSetAdded`/`OnAttributeSetRemoved` if the observer has to survive that.
11. **Probe with `TryGetAttribute`**: The indexer throws for an unknown key. Anywhere an attribute might not be present — optional sets, or sets that come and go — probe rather than index.
