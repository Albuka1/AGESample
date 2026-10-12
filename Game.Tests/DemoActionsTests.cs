using Age.Core;
using Age.Input;
using Age.Physics;
using Age.Rendering;
using FluentAssertions;
using Xunit;

namespace Age.Platformer.Tests;

/// <summary>
/// Pins the behaviour the demo is built from to what a person does with a keyboard, without opening a window: the sprite moves by the
/// speed of the config, a contact tints it and letting go restores it, and a spawn carries a timer of the engine rather than a counter
/// of the game.
/// </summary>
/// <remarks>
/// The keys are driven through <see cref="InputStateTracker"/>, which is the backend-independent half of the input of the engine: a
/// test reports a key going down and the code under test reads it exactly as it reads a keyboard.
/// </remarks>
public sealed class DemoActionsTests
{
    [Fact]
    public void Move_NoKeyIsHeld_LeavesTheEntityWhereItStands()
    {
        World world = Build(out Entity hero);
        Vector2 before = world.Get<TransformComponent>(hero).Position;

        DemoActions.Move(world, hero, new InputStateTracker(), new GameTime(1d, 1d));

        world.Get<TransformComponent>(hero).Position.Should().Be(before);
    }

    [Fact]
    public void Move_D_OfOneSecond_TravelsTheDistanceOfTheConfig()
    {
        World world = Build(out Entity hero);
        Vector2 before = world.Get<TransformComponent>(hero).Position;

        DemoActions.Move(world, hero, Held(Key.D), new GameTime(1d, 1d));

        world.Get<TransformComponent>(hero).Position.X.Should().BeApproximately(before.X + DemoConfig.MoveSpeed, 0.01f);
    }

    [Fact]
    public void Move_ADeadEntity_IsIgnoredRatherThanThrow()
    {
        World world = Build(out Entity hero);
        world.DestroyEntity(hero);

        Action move = () => DemoActions.Move(world, hero, Held(Key.D), new GameTime(1d, 1d));

        move.Should().NotThrow("a system of a game runs over entities a scene may have destroyed");
    }

    [Fact]
    public void TintByCollision_AContact_RepaintsBothAndRestoringPutsTheCalmColourBack()
    {
        World world = Build(out Entity hero);
        Entity other = world.CreateEntity();
        world.Set(other, new SpriteComponent { Size = new Vector2(32f, 32f), Color = Color.Green });

        world.Set(hero, new CollisionComponent());
        DemoActions.TintByCollision(world, hero, other);
        world.Get<SpriteComponent>(hero).Color.Should().NotBe(Color.White, "a contact is what a person is meant to see");

        world.Remove<CollisionComponent>(hero);
        DemoActions.RestoreCalmColour(world, hero, Color.White);
        world.Get<SpriteComponent>(hero).Color.Should().Be(Color.White, "the calm colour comes back when the contact ends");
    }

    [Fact]
    public void SpawnFading_CarriesATimerOfTheEngine_SoTheWorldDestroysItRatherThanTheGame()
    {
        var world = new World();

        Entity spawned = DemoActions.SpawnFading(world, new Vector2(10f, 20f), DemoConfig.SpawnLifetime);

        world.IsAlive(spawned).Should().BeTrue();
        world.Has<TimerComponent>(spawned).Should().BeTrue("the lifetime is a timer of the engine, not a counter of the game");
        world.Get<TimerComponent>(spawned).Remaining.Should().BeApproximately(DemoConfig.SpawnLifetime, 0.001f);
    }

    [Fact]
    public void FirstSprite_OfAWorldThatHoldsOne_AnswersIt()
    {
        World world = Build(out Entity hero);

        DemoActions.FirstSprite(world).Should().Be(hero);
    }

    [Fact]
    public void SpawnMissing_NamesAnImageTheBuildDoesNotShip_WhichIsWhatThePlaceholderIsFor()
    {
        var world = new World();

        Entity missing = DemoActions.SpawnMissing(world, new Vector2(5f, 5f));

        world.Get<SpriteComponent>(missing).TexturePath.Should().NotBeNullOrWhiteSpace();
    }


    /// <summary>Builds a world with the sprite the demo moves by hand in it.</summary>
    private static World Build(out Entity hero)
    {
        var world = new World();
        hero = world.CreateEntity();
        world.Set(hero, new TransformComponent { Position = new Vector2(400f, 300f), Scale = new Vector2(1f, 1f) });
        world.Set(hero, new SpriteComponent { Size = new Vector2(64f, 64f), Color = Color.White });
        return world;
    }

    /// <summary>Returns an input that reports one key as held, which is what a test of movement needs.</summary>
    private static InputStateTracker Held(Key key)
    {
        var input = new InputStateTracker();
        input.BeginFrame();
        input.KeyDown(key);
        return input;
    }

}

