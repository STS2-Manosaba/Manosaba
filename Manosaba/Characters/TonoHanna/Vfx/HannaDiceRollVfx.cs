using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Manosaba.Characters.TonoHanna.Vfx;

/// <summary>
/// Renders a real 3D cube in a small transparent viewport and tumbles it to the supplied face.
/// Numbered face art is loaded from face_1.png through face_6.png; the Ascension Elevator card uses its own art for faces 5 and 6.
/// </summary>
public partial class HannaDiceRollVfx : Node2D
{
    private const string FacePathFormat = "res://Manosaba/images/characters/tono_hanna/dice/face_{0}.png";
    private const string ElevatorFacePathFormat = "res://Manosaba/images/characters/tono_hanna/dice/face_{0}_{1}.png";
    private const int FaceCount = 6;
    private const int ViewportSize = 256;
    private const float RollDuration = 1.25f;
    private const float FrameDelay = 0.016f;

    private static readonly Vector3[] FacePositions =
    [
        new(0f, 0f, 0.505f),
        new(0.505f, 0f, 0f),
        new(0f, 0.505f, 0f),
        new(0f, 0f, -0.505f),
        new(0f, -0.505f, 0f),
        new(-0.505f, 0f, 0f),
    ];

    private static readonly Vector3[] FaceRotations =
    [
        Vector3.Zero,
        new(0f, -Mathf.Pi / 2f, 0f),
        new(Mathf.Pi / 2f, 0f, 0f),
        new(0f, Mathf.Pi, 0f),
        new(-Mathf.Pi / 2f, 0f, 0f),
        new(0f, Mathf.Pi / 2f, 0f),
    ];

    /// <summary>Plays a 3D roll at the center of the current combat viewport. The result is one-based (1-6).</summary>
    public static Task PlayAtCombatCenterAsync(int result) => PlayAtCombatCenterAsync([result]);

    /// <summary>Plays two dice together, side by side, and settles both on their supplied results.</summary>
    public static Task PlayTwoAtCombatCenterAsync(int firstResult, int secondResult) =>
        PlayAtCombatCenterAsync([firstResult, secondResult]);

    /// <summary>Plays three numbered dice together, side by side, and settles them on their supplied results.</summary>
    public static Task PlayThreeNumberedAtCombatCenterAsync(
        int firstResult,
        int secondResult,
        int thirdResult,
        bool useFourToSixFaces) =>
        PlayAtCombatCenterAsync(
            [firstResult, secondResult, thirdResult],
            useElevatorFaces: false,
            useFourToSixFaces: useFourToSixFaces);

    private static async Task PlayAtCombatCenterAsync(
        IReadOnlyList<int> results,
        bool useElevatorFaces = true,
        bool useFourToSixFaces = false)
    {
        if (results.Count == 0 || results.Any(result => result is < 1 or > FaceCount))
        {
            throw new ArgumentOutOfRangeException(nameof(results), "Each dice result must be from 1 to 6.");
        }

        NCombatRoom? room = NCombatRoom.Instance;
        NGame? game = NGame.Instance;
        if (room == null || game == null)
        {
            return;
        }

        Vector2 center = game.GetViewportRect().Size * 0.5f;
        var animations = new List<Task>(results.Count);
        for (int i = 0; i < results.Count; i++)
        {
            HannaDiceRollVfx vfx = new();
            room.CombatVfxContainer.AddChildSafely(vfx);
            float horizontalOffset = (i - (results.Count - 1) * 0.5f) * (ViewportSize + 24f);
            vfx.GlobalPosition = center + new Vector2(horizontalOffset, 0f);
            animations.Add(vfx.PlayAnimationAsync(results[i], useElevatorFaces, useFourToSixFaces));
        }

        await Task.WhenAll(animations);
    }

    private async Task PlayAnimationAsync(int result, bool useElevatorFaces, bool useFourToSixFaces)
    {
        Node3D die = Create3DViewportAndDie(useElevatorFaces, useFourToSixFaces);
        int resultFaceIndex = useFourToSixFaces ? result - 4 : result - 1;
        Vector3 targetRotation = FaceRotations[resultFaceIndex];

        // Several full turns on all three axes create a tumbling throw; the keyed result determines the landing face.
        Vector3 fullTurns = new(
            Mathf.Tau * (2 + result % 2),
            -Mathf.Tau * (3 + result % 3),
            Mathf.Tau * (2 + (result + 1) % 3));
        Vector3 startRotation = targetRotation + fullTurns + new Vector3(0.28f, -0.2f, 0.16f);
        die.Rotation = startRotation;
        die.Position = new Vector3(0f, 1.15f, 0.25f);

        float elapsed = 0f;
        while (elapsed < RollDuration && GodotObject.IsInstanceValid(die) && IsInsideTree())
        {
            float progress = Mathf.Clamp(elapsed / RollDuration, 0f, 1f);
            float eased = 1f - Mathf.Pow(1f - progress, 2.4f);
            die.Rotation = startRotation.Lerp(targetRotation, eased);

            float bounce = Mathf.Sin(progress * Mathf.Pi * 3f) * (1f - progress) * 0.22f;
            die.Position = new Vector3(
                Mathf.Sin(progress * Mathf.Pi * 2f) * (1f - progress) * 0.22f,
                (1f - eased) * 1.15f + Mathf.Abs(bounce),
                (1f - eased) * 0.25f);

            await ToSignal(GetTree().CreateTimer(FrameDelay), SceneTreeTimer.SignalName.Timeout);
            elapsed += FrameDelay;
        }

        if (GodotObject.IsInstanceValid(die))
        {
            die.Rotation = targetRotation;
            die.Position = Vector3.Zero;
        }

        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }

    private Node3D Create3DViewportAndDie(bool useElevatorFaces, bool useFourToSixFaces)
    {
        var viewport = new SubViewport
        {
            Size = new Vector2I(ViewportSize, ViewportSize),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            World3D = new World3D()
        };
        AddChild(viewport);

        var output = new Sprite2D
        {
            Texture = viewport.GetTexture(),
            Centered = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            Scale = Vector2.One * 0.94f,
            ZIndex = 200
        };
        AddChild(output);

        var camera = new Camera3D
        {
            Position = new Vector3(0f, 0f, 3.3f),
            Fov = 38f,
            Current = true
        };
        viewport.AddChild(camera);

        var light = new DirectionalLight3D
        {
            Rotation = new Vector3(-0.62f, -0.48f, 0f),
            LightEnergy = 1.6f
        };
        viewport.AddChild(light);

        var die = new Node3D();
        viewport.AddChild(die);

        var cube = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = Vector3.One },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color("fff1d8"),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        };
        die.AddChild(cube);

        for (int face = 1; face <= FaceCount; face++)
        {
            int faceValue = useFourToSixFaces ? (face - 1) % 3 + 4 : face;
            AddDieFace(die, face, faceValue, useElevatorFaces);
        }

        return die;
    }

    private static void AddDieFace(Node3D die, int faceIndex, int faceValue, bool useElevatorFaces)
    {
        string texturePath = useElevatorFaces && faceValue == 5
            ? string.Format(ElevatorFacePathFormat, faceValue, "elevator")
            : useElevatorFaces && faceValue == 6
                ? string.Format(ElevatorFacePathFormat, faceValue, "eel")
                : string.Format(FacePathFormat, faceValue);
        Texture2D? texture = ResourceLoader.Exists(texturePath) ? GD.Load<Texture2D>(texturePath) : null;
        var material = new StandardMaterial3D
        {
            AlbedoColor = faceIndex % 2 == 0 ? new Color("fff8e9") : new Color("fff1d8"),
            AlbedoTexture = texture,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        var surface = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(0.96f, 0.96f) },
            MaterialOverride = material,
            Position = FacePositions[faceIndex - 1],
            Rotation = FaceRotations[faceIndex - 1]
        };
        die.AddChild(surface);

        if (texture == null)
        {
            AddFallbackFaceMarkings(surface, faceValue, useElevatorFaces);
        }
    }

    private static void AddFallbackFaceMarkings(Node3D surface, int face, bool useElevatorFaces)
    {
        if (useElevatorFaces && face >= 5)
        {
            var label = new Label3D
            {
                Text = face == 5 ? "LIFT" : "EEL",
                FontSize = 48,
                PixelSize = 0.006f,
                Modulate = face == 5 ? new Color("b43c35") : new Color("247e9b"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            surface.AddChild(label);
            return;
        }

        Vector2[] pips = face switch
        {
            1 => [Vector2.Zero],
            2 => [new(-0.22f, -0.22f), new(0.22f, 0.22f)],
            3 => [new(-0.22f, -0.22f), Vector2.Zero, new(0.22f, 0.22f)],
            4 => [new(-0.22f, -0.22f), new(0.22f, -0.22f), new(-0.22f, 0.22f), new(0.22f, 0.22f)],
            5 => [new(-0.22f, -0.22f), new(0.22f, -0.22f), Vector2.Zero, new(-0.22f, 0.22f), new(0.22f, 0.22f)],
            _ => [new(-0.22f, -0.22f), new(0.22f, -0.22f), new(-0.22f, 0.22f), new(0.22f, 0.22f), new(-0.22f, 0f), new(0.22f, 0f)]
        };
        var pipMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("49332e"),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };
        foreach (Vector2 pip in pips)
        {
            var mark = new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.07f, Height = 0.14f },
                MaterialOverride = pipMaterial,
                Position = new Vector3(pip.X, pip.Y, 0.015f)
            };
            surface.AddChild(mark);
        }
    }
}
