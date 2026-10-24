# Input

An entity that a player controls reads its input from an `EntityInput`: one frame of input per tick, collected by the host and consumed by the [simulation tick](simulation.md). Abilities can bind to input actions and activate on their own, in the tick the action is pressed, and Statescript graphs can read the same frame.

Input is raw and small on purpose. A frame says which buttons are down, which were pressed or released since the last tick, a few analog vectors and where the player aims, and nothing else. Every decision the input leads to is made by the simulation, the same way on every machine that runs it.

## The action set

An `InputActionSet` names the actions a frame carries, in a fixed order. The order gives each action its slot, and every machine that simulates the entity must use the same set, so a slot means the same action everywhere.

```csharp
var actions = new InputActionSet(
    ["jump", "dash", "fire"],
    [new InputVector("move_left", "move_right", "move_forward", "move_back")]);
```

- **Actions** travel as buttons, one bit each, up to 64. A name may appear once.
- **Vectors** carry analog values, up to 4. Each one reads four actions as a two-dimensional value, such as a movement stick. The four actions don't need to be in the action list as well.

An action that is part of no vector has no analog value. It reads as fully on while held and off otherwise.

## Frames

An `InputFrame` is one tick of input. Its action bits use the action set's slots:

| Member | Meaning |
|---|---|
| `Held` | The actions down when the input was collected. |
| `Pressed` | The actions pressed since the last tick. |
| `Released` | The actions released since the last tick. |
| `GetVector(slot)` | An analog vector, each component between -1 and 1. |
| `HasAim`, `AimPoint`, `AimDirection` | Where the entity aims, in world space. |

A press and a release that both happen between two ticks set both bits with the action no longer held. A tap shorter than a tick still reaches the simulation.

Vectors and aim are stored already quantized: vectors in steps of 1/127, aim points in thousandths of a unit and aim directions in steps of 1/32767. `WithVector` and `WithAim` round as they store. The machine that collects the input therefore simulates exactly the values every other machine will receive, and quantizing a frame again changes nothing.

```csharp
InputFrame frame = new InputFrame { Held = 0b001, Pressed = 0b001 }
    .WithVector(0, new Vector2(0.5f, -1f))
    .WithAim(targetPoint, aimDirection);
```

## An entity's input

An entity exposes its input through `IForgeEntity.Input`. It's a default member that returns `null`, so existing entities are unaffected, and an entity nobody controls through input keeps returning `null`.

```csharp
public class Hero : IForgeEntity
{
    // ...the other IForgeEntity members...

    public EntityInput Input { get; } = new(actions);
}
```

Whatever collects input calls `Submit` between ticks. `EntitySimulation.Tick` makes the submitted frame current as the tick begins:

- **Several submissions before a tick are merged.** The latest frame's state stands, and every press and release any of them saw is kept.
- **A tick with nothing submitted** keeps the last frame's state with no new presses or releases, so held actions stay held.

Game code reads the current frame directly (`input.Current`) or through readers that take names and answer the way an engine's input API does:

| Reader | Answers |
|---|---|
| `IsHeld`, `WasPressed`, `WasReleased` | The action's bits this tick. An action the set doesn't carry reads as never used. |
| `GetStrength(action)` | Between 0 and 1: from a vector that has the action as one of its four directions, or otherwise 1 while held. |
| `GetAxis(negative, positive)` | Between -1 and 1: a vector's component when a vector has the two as an axis, or otherwise the difference of their strengths. |
| `GetVector(negativeX, positiveX, negativeY, positiveY)` | The vector the set carries for these four actions, or otherwise one built from the two axes, no longer than 1. |

## Ability input bindings

An ability can bind to an input action through `AbilityData.InputBinding`. In the tick its owner's input reports the action's edge, the ability tries to activate:

```csharp
var dash = new AbilityData(
    "Dash",
    behaviorFactory: () => new DashBehavior(),
    inputBinding: AbilityInputBinding.ForAction("dash"));
```

- **The edge** is `InputEdge.Pressed` by default. `InputEdge.Released` activates on the release instead, for a release-to-fire shot. Holding the action never activates the ability again.
- **Activation data** comes from the same tick's input. The generic overload takes a function that builds it from the owner and its input, and activates the ability with that data:

  ```csharp
  AbilityInputBinding.ForAction(
      "fire",
      (owner, input) => new AimData(input.Current.AimPoint, input.Current.AimDirection));
  ```

- **No input, no activation.** A binding fires only for an entity whose `Input` isn't `null` and whose action set carries the action.

A failed activation, such as one still on cooldown, is simply skipped. The ability's activation rules apply as they do for any other activation.

## Reading input in graphs

Statescript graphs read input in two ways:

- **[InputActionNode](statescript/nodes/state/input-action-node.md)** waits on a button. It reports presses and releases and keeps a subgraph running while the button is held, for channels, charged shots and combo windows.
- **[Input resolvers](statescript/resolvers/README.md#input-resolvers)** read values: whether an action is held, pressed or released, its strength, an axis, a vector, or the aim.

Both read the current frame of the ability owner's input, or of another entity bound as the input source. The node reads on the fixed rail, once per tick, so it sees every edge a tick carries. An entity without an input isn't read: the node reports nothing and the resolvers resolve to default values.

```csharp
var watch = new InputActionNode("fire");
var aim = new InputAimResolver(InputAimValue.Point3D);
var move = new InputVectorResolver("move_left", "move_right", "move_forward", "move_back");
```

## Where input fits in the tick

`EntitySimulation.BeginTick` makes the submitted frame current, alongside restarting the entity's random source, so a graph on another entity reads this tick's frame whichever entity advances first. `EntitySimulation.Advance` starts with the input stage, taking the frame itself if `BeginTick` didn't, and a frame is taken once per tick however often `BeginTick` is called. Then effects advance, then the fixed rail. An ability activated by input therefore takes its first fixed step in the same tick, and a graph that reads the entity's input on the fixed rail reads the frame of the tick it runs in. See [Simulation Tick](simulation.md#advancing-an-entity).
