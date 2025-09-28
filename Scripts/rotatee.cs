using System;
using Godot;

public partial class rotatee : Label3D
{
    private Vector3 Rot;
    [Export] Camera3D panorama;
    private float vel = 670f;
    private float ry;
    private float rx;

    public override void _Ready()
    {
        ry = 1 - (GD.Randf() * 2f); 
        rx = 1 - (GD.Randf() * 2f); 
        Rot = Rotation;
    }

    public override void _PhysicsProcess(double delta)
    {
        vel *= 0.96f + (float)delta;
        Rot.Y += ry * (float)delta * (1+vel);
        Rot.Z += rx * (float)delta * (1+vel);
        panorama.RotateY(0.0025f);
        Rotation = Rot;
    }
}
