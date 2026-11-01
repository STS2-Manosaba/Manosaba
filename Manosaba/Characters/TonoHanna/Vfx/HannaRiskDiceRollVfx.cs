using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Manosaba.Characters.TonoHanna.Vfx;

/// <summary>Renders and tumbles a 20-face triangular die, updating the lucky/unlucky face boundary as the chance changes.</summary>
public partial class HannaRiskDiceRollVfx : Node2D
{
    private const int FaceCount = 20;
    private const int ViewportSize = 360;
    private const float RollDuration = 1.25f;
    private const float FrameDelay = 0.016f;
    private const float DieRadius = 1.35f;
    private const string GreatLuckFacePath = "res://Manosaba/images/characters/tono_hanna/dice/risk_dice_great_luck.png";
    private const string GreatMisfortuneFacePath = "res://Manosaba/images/characters/tono_hanna/dice/risk_dice_great_misfortune.png";
    private static Texture2D? _greatLuckTexture;
    private static Texture2D? _greatMisfortuneTexture;
    private static bool _faceTexturesLoaded;

    private static readonly Vector3[] UnitVertices = CreateUnitVertices();
    private static readonly int[][] FaceIndices =
    [
        [0, 11, 5], [0, 5, 1], [0, 1, 7], [0, 7, 10], [0, 10, 11],
        [1, 5, 9], [5, 11, 4], [11, 10, 2], [10, 7, 6], [7, 1, 8],
        [3, 9, 4], [3, 4, 2], [3, 2, 6], [3, 6, 8], [3, 8, 9],
        [4, 9, 5], [2, 4, 11], [6, 2, 10], [8, 6, 7], [9, 8, 1]
    ];

    /// <summary>Rolls the 20-face die and leaves the requested face pointing toward the combat camera.</summary>
    public static async Task PlayAtCombatCenterAsync(int result, int goodFaceCount)
    {
        if (result is < 1 or > FaceCount)
        {
            throw new ArgumentOutOfRangeException(nameof(result), "The risk die result must be from 1 to 20.");
        }

        if (goodFaceCount is < 0 or >= FaceCount)
        {
            throw new ArgumentOutOfRangeException(nameof(goodFaceCount), "The risk die must have between 0 and 19 lucky faces.");
        }

        NCombatRoom? room = NCombatRoom.Instance;
        NGame? game = NGame.Instance;
        if (room == null || game == null)
        {
            return;
        }

        var animation = new HannaRiskDiceRollVfx();
        room.CombatVfxContainer.AddChildSafely(animation);
        animation.GlobalPosition = game.GetViewportRect().Size * 0.5f;
        await animation.PlayAnimationAsync(result, goodFaceCount);
    }

    private async Task PlayAnimationAsync(int result, int goodFaceCount)
    {
        var viewport = new SubViewport
        {
            Size = new Vector2I(ViewportSize, ViewportSize),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            World3D = new World3D()
        };
        AddChild(viewport);

        AddChild(new Sprite2D
        {
            Texture = viewport.GetTexture(),
            Centered = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            Scale = Vector2.One * 0.9f,
            ZIndex = 200
        });

        viewport.AddChild(new Camera3D
        {
            Position = new Vector3(0f, 0f, 4.8f),
            Fov = 38,
            Current = true
        });
        viewport.AddChild(new DirectionalLight3D
        {
            Rotation = new Vector3(-0.62f, -0.48f, 0f),
            LightEnergy = 1.8f
        });
        viewport.AddChild(new OmniLight3D
        {
            Position = new Vector3(-2f, 2f, 3f),
            LightEnergy = 0.7f,
            OmniRange = 8f
        });

        var die = new Node3D();
        viewport.AddChild(die);
        AddFaces(die, goodFaceCount);

        Vector3 faceNormal = GetFaceNormal(result - 1);
        Quaternion targetQuaternion = new(faceNormal, Vector3.Back);
        Vector3 targetRotation = targetQuaternion.GetEuler();
        Vector3 fullTurns = new(
            Mathf.Tau * (2 + result % 2),
            -Mathf.Tau * (3 + result % 3),
            Mathf.Tau * (2 + (result + 1) % 3));
        Vector3 startRotation = targetRotation + fullTurns + new Vector3(0.28f, -0.2f, 0.16f);
        die.Rotation = startRotation;
        die.Position = new Vector3(0f, 0.75f, 0.2f);

        float elapsed = 0f;
        while (elapsed < RollDuration && GodotObject.IsInstanceValid(die) && IsInsideTree())
        {
            float progress = Mathf.Clamp(elapsed / RollDuration, 0f, 1f);
            float eased = 1f - Mathf.Pow(1f - progress, 2.4f);
            die.Rotation = startRotation.Lerp(targetRotation, eased);
            float bounce = Mathf.Sin(progress * Mathf.Pi * 3f) * (1f - progress) * 0.3f;
            die.Position = new Vector3(
                Mathf.Sin(progress * Mathf.Pi * 2f) * (1f - progress) * 0.2f,
                (1f - eased) * 0.75f + Mathf.Abs(bounce),
                (1f - eased) * 0.2f);

            await ToSignal(GetTree().CreateTimer(FrameDelay), SceneTreeTimer.SignalName.Timeout);
            elapsed += FrameDelay;
        }

        if (GodotObject.IsInstanceValid(die))
        {
            die.Rotation = targetRotation;
            die.Position = Vector3.Zero;
        }

        await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }

    private static void AddFaces(Node3D die, int goodFaceCount)
    {
        var mesh = new ArrayMesh();
        var normals = new Vector3[FaceCount];
        LoadFaceTextures();

        for (int faceIndex = 0; faceIndex < FaceCount; faceIndex++)
        {
            Vector3 a = UnitVertices[FaceIndices[faceIndex][0]] * DieRadius;
            Vector3 b = UnitVertices[FaceIndices[faceIndex][1]] * DieRadius;
            Vector3 c = UnitVertices[FaceIndices[faceIndex][2]] * DieRadius;
            Vector3 normal = (b - a).Cross(c - a).Normalized();
            if (normal.Dot((a + b + c) / 3f) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }

            normals[faceIndex] = normal;
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = new[] { a, b, c };
            arrays[(int)Mesh.ArrayType.Normal] = new[] { normal, normal, normal };
            arrays[(int)Mesh.ArrayType.TexUV] = new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f) };
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

            bool isBadFace = faceIndex >= goodFaceCount;
            Texture2D? faceTexture = isBadFace ? _greatMisfortuneTexture : _greatLuckTexture;
            var material = new StandardMaterial3D
            {
                AlbedoTexture = faceTexture,
                AlbedoColor = faceTexture != null
                    ? Colors.White
                    : isBadFace
                        ? new Color("b83c38")
                        : (faceIndex % 2 == 0 ? new Color("fff0d0") : new Color("e6c995")),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Metallic = 0f,
                Roughness = 1f,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            };
            mesh.SurfaceSetMaterial(faceIndex, material);
        }

        die.AddChild(new MeshInstance3D { Mesh = mesh });

        for (int faceIndex = 0; faceIndex < FaceCount; faceIndex++)
        {
            Vector3 a = UnitVertices[FaceIndices[faceIndex][0]] * DieRadius;
            Vector3 b = UnitVertices[FaceIndices[faceIndex][1]] * DieRadius;
            Vector3 c = UnitVertices[FaceIndices[faceIndex][2]] * DieRadius;
            Vector3 center = (a + b + c) / 3f;
            Vector3 normal = normals[faceIndex];
            bool isBadFace = faceIndex >= goodFaceCount;
            bool hasFaceTexture = isBadFace ? _greatMisfortuneTexture != null : _greatLuckTexture != null;
            if (hasFaceTexture)
            {
                continue;
            }

            var label = new Label3D
            {
                Text = isBadFace ? "大凶" : "大吉",
                FontSize = 36,
                PixelSize = 0.009f,
                OutlineSize = 6,
                OutlineModulate = new Color("382824"),
                Modulate = new Color("382824"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                NoDepthTest = false
            };
            label.Position = center + normal * 0.025f;
            label.Quaternion = new Quaternion(Vector3.Forward, normal);
            die.AddChild(label);
        }
    }

    private static void LoadFaceTextures()
    {
        if (_faceTexturesLoaded)
        {
            return;
        }

        _greatLuckTexture = LoadReadableFaceTexture(GreatLuckFacePath, isBadFace: false);
        _greatMisfortuneTexture = LoadReadableFaceTexture(GreatMisfortuneFacePath, isBadFace: true);
        _faceTexturesLoaded = true;
    }

    /// <summary>Replace the source art's black face with a light die face and retain its lucky/unlucky mark as a high contrast ink.</summary>
    private static Texture2D? LoadReadableFaceTexture(string path, bool isBadFace)
    {
        if (!ResourceLoader.Exists(path) || GD.Load<Texture2D>(path) is not { } sourceTexture)
        {
            return null;
        }

        Image source = sourceTexture.GetImage();
        if (source.IsCompressed() && source.Decompress() != Error.Ok)
        {
            return sourceTexture;
        }

        Color background = isBadFace ? new Color("f4d8d3") : new Color("f3dfb7");
        Color ink = isBadFace ? new Color("a7192c") : new Color("51351f");
        Image composed = Image.CreateEmpty(source.GetWidth(), source.GetHeight(), false, Image.Format.Rgba8);
        for (int y = 0; y < source.GetHeight(); y++)
        {
            for (int x = 0; x < source.GetWidth(); x++)
            {
                Color pixel = source.GetPixel(x, y);
                float mask;
                if (pixel.A <= 0f)
                {
                    mask = 0f;
                }
                else if (isBadFace)
                {
                    float nonRed = Math.Max(pixel.G, pixel.B);
                    float redInk = Mathf.Clamp((pixel.R - nonRed - 0.04f) * 5f, 0f, 1f);
                    float brightness = Mathf.Clamp((pixel.R - 0.1f) * 1.5f, 0f, 1f);
                    mask = redInk * brightness;
                }
                else
                {
                    float brightness = (pixel.R + pixel.G + pixel.B) / 3f;
                    mask = Mathf.Clamp((brightness - 0.08f) * 1.25f, 0f, 1f);
                }

                Color output = background.Lerp(ink, mask);
                output.A = 1f;
                composed.SetPixel(x, y, output);
            }
        }

        return ImageTexture.CreateFromImage(composed);
    }

    private static Vector3 GetFaceNormal(int faceIndex)
    {
        Vector3 a = UnitVertices[FaceIndices[faceIndex][0]];
        Vector3 b = UnitVertices[FaceIndices[faceIndex][1]];
        Vector3 c = UnitVertices[FaceIndices[faceIndex][2]];
        Vector3 normal = (b - a).Cross(c - a).Normalized();
        return normal.Dot((a + b + c) / 3f) < 0f ? -normal : normal;
    }

    private static Vector3[] CreateUnitVertices()
    {
        float phi = (1f + Mathf.Sqrt(5f)) * 0.5f;
        Vector3[] vertices =
        [
            new(-1, phi, 0), new(1, phi, 0), new(-1, -phi, 0), new(1, -phi, 0),
            new(0, -1, phi), new(0, 1, phi), new(0, -1, -phi), new(0, 1, -phi),
            new(phi, 0, -1), new(phi, 0, 1), new(-phi, 0, -1), new(-phi, 0, 1)
        ];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = vertices[i].Normalized();
        }

        return vertices;
    }
}
