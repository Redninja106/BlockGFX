using ConsoleApp31.Drawing;
using GLFW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;

internal class Camera
{
    public readonly MatrixBuilder View = new();
    public readonly MatrixBuilder Projection = new();

    public Transform Transform = new();

    public float AspectRatio { get; private set; }

    public int DisplayWidth { get; private set; }
    public int DisplayHeight { get; private set; }

    public float NearPlane { get; set; }
    public float FarPlane { get; set; }
    public float FieldOfView { get; set; }
    public bool IsStatic => false;

    private Vector2 lastMousePos;
    private float xr, yr;
    private bool mouseCaptured;

    private Vector3 velocity;

    private float gravity = -32;
    private float jumpHeight = 1.25f;
    private float walkSpeed = 4.3f;
    private float acceleration = 5f;
    private float verticalDrag = .98f;
    private float horizontalDrag = .9f;
    private bool isGrounded = false;

    public Camera(float nearPlane, float farPlane, float fieldOfView)
    {
        NearPlane = nearPlane;
        FarPlane = farPlane;
        FieldOfView = fieldOfView;
    }

    public void Update(float dt)
    {
        this.DisplayWidth = Graphics.RenderTargetWidth;
        this.DisplayHeight = Graphics.RenderTargetHeight;

        AspectRatio = DisplayWidth / (float)DisplayHeight;

        if (dt > (1 / 30f))
            dt = 1 / 30f;

        if (Input.IsMouseButtonPressed(MouseButton.Middle))
            mouseCaptured = !mouseCaptured;

        if (mouseCaptured && Input.WindowFocused)
        {
            if (Input.IsMouseButtonPressed(MouseButton.Middle))
            {
                lastMousePos = Input.MousePosition;
            }

            var mouseDelta = Input.MousePosition - lastMousePos;

            yr -= mouseDelta.X * 0.0005f * MathF.PI;
            xr += mouseDelta.Y * 0.0005f * MathF.PI;

            xr = Math.Clamp(xr, -MathF.PI / 2f + 0.001f, MathF.PI / 2f - 0.001f);

            Transform.Rotation = Quaternion.Concatenate(
                    Quaternion.CreateFromAxisAngle(Vector3.UnitX, xr),
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, yr)
                );

            Input.SetMousePosition(new(DisplayWidth / 2, DisplayHeight / 2));
            lastMousePos = new Vector2(DisplayWidth / 2, DisplayHeight / 2);
        }

        // Input.MouseVisible = !mouseCaptured;

        Vector3 deltaXZ = Vector3.Zero;

        if (Input.IsKeyDown(Keys.W))
            deltaXZ += Vector3.UnitZ;

        if (Input.IsKeyDown(Keys.S))
            deltaXZ -= Vector3.UnitZ;

        if (Input.IsKeyDown(Keys.A))
            deltaXZ += Vector3.UnitX;

        if (Input.IsKeyDown(Keys.D))
            deltaXZ -= Vector3.UnitX;

        if (isGrounded && Input.IsKeyPressed(Keys.Space))
        {
            float jumpForce = MathF.Sqrt(-2 * gravity * jumpHeight);

            velocity.Y += jumpForce;
        }

        if (Input.IsKeyDown(Keys.LeftShift))
            deltaXZ *= 5;

        Vector3 targetDir = walkSpeed * Vector3.Transform(deltaXZ.Normalized(), Matrix4x4.CreateRotationY(yr));

        var chunkManager = Program.World.Components.OfType<BlockChunkManager>().Single();

        Vector3 a = targetDir - velocity * new Vector3(1, 0, 1);

        if (chunkManager.GetChunk(new BlockCoordinate(this.Transform.Position).ToChunkCoordinate()) is not null)
        {
            velocity += a * acceleration * dt;
            velocity += Vector3.UnitY * gravity * dt;

            velocity.Y *= MathF.Pow(verticalDrag, dt);
            velocity.X *= MathF.Pow(horizontalDrag, dt);
            velocity.Z *= MathF.Pow(horizontalDrag, dt);

            TryMove(velocity * Vector3.UnitX * dt);
            TryMove(velocity * Vector3.UnitY * dt);
            TryMove(velocity * Vector3.UnitZ * dt);
        }

        void TryMove(Vector3 movement)
        {
            var newTransform = this.Transform;
            newTransform.Translate(movement);
            var collider = GetCollider();

            if (!chunkManager.Intersect(collider.Translated(newTransform.Position - this.Transform.Position), out _))
            {
                if (movement.Y != 0)
                    isGrounded = false;

                this.Transform = newTransform;
            }
            else
            {
                velocity *= Vector3.One - Vector3.Abs(movement.Normalized());

                if (movement.Y != 0)
                    isGrounded = true;
            }
        }

        Projection
            .Reset()
            .PerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);

        View
            .Reset()
            .LookAt(Transform.Position, Transform.Position + Transform.Forward, Vector3.UnitY);

        if (Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsMouseButtonPressed(MouseButton.Right))
        {
            bool breaking = Input.IsMouseButtonPressed(MouseButton.Right);

            Ray ray = new(Transform, 8);
            if (chunkManager.Raycast(ray, out var hit))
            {
                Vector3 block = hit.box.min;

                if (!breaking)
                {
                    block += hit.normal;
                }

                var collider = GetCollider();

                if (breaking || !collider.Intersect(new(block, block + Vector3.One), out _))
                    chunkManager.TrySetBlock(new(block), new(breaking ? 0 : 3));
            }
        }

        const int viewDistance = 5;

        ChunkCoordinate baseCoordinate = new(this.Transform.Position / BlockChunk.SizeVector);

        for (int y = -viewDistance; y < viewDistance; y++)
        {
            for (int x = -viewDistance; x < viewDistance; x++)
            {
                for (int z = -viewDistance; z < viewDistance; z++)
                {
                    ChunkCoordinate coordinate = new(x + baseCoordinate.X, y + baseCoordinate.Y, z + baseCoordinate.Z);

                    var chunk = chunkManager.GetChunk(coordinate);

                    if (chunk is null)
                    {
                        if (Vector3.Distance(coordinate.ToVector3(), baseCoordinate.ToVector3()) < viewDistance)
                        {
                            chunkManager.AddChunk(coordinate);
                            return;
                        }
                    }
                }
            }
        }

        foreach (var (_, chunk) in chunkManager.Chunks)
        {
            if (Vector3.Distance(chunk.location.ToVector3(), baseCoordinate.ToVector3()) > viewDistance)
            {
                chunkManager.RemoveChunk(chunk.location);
            }
        }
    }

    Box GetCollider()
    {
        return new Box(this.Transform.Position - Vector3.One * .2f - Vector3.UnitY * 1.375f, this.Transform.Position + Vector3.One * .2f);
    }
}