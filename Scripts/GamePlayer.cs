using System;
using System.Xml;
using Godot;
public partial class GamePlayer : CharacterBody3D
{
    [Export] public Camera3D Camera;
    [Export] public bool isClient;
    [Export] public Node3D Plm;
    public Vector3 pos;
    public Vector3 rot;
    public float min_zoom = 0.0f;
	public float max_zoom = 45.0f;
	public float zoom = 0.0f;
    [Export] public Label3D Label;
    [Export] private Node3D xBone;
    [Export] private Node3D yBone;
    [Export] public Multiplayer Client;
    [Export] public Timer PollingRate;
    [Export] public CanvasLayer UI;
    [Export] public RichTextLabel Chat;
    [Export] public LineEdit Text;
    [Export] public bool FocusTester = false;
    [Export] public Button FocusTesterButton;
    [Export] private PackedScene LeaderboardName;
    [Export] private VBoxContainer LeaderboardList;
    public const float JumpVelocity = 10.0f;
    public const float Speed = 3.5f;
    public override void _Ready()
    {
        FocusTesterButton.GrabFocus();
        FocusTester = true;
    }
    public override void _Input(InputEvent @event)
    {
        if (isClient || FocusTester)
        {
            if (@event is InputEventMouseMotion mouseEvent)
            {
                if (Input.IsActionPressed("RC") || zoom <= 0.5f)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    Vector2 rotationAmount = mouseEvent.Relative * 2.5f;
                    xBone.RotateY(Mathf.DegToRad(-rotationAmount.X / 2.75f));
                    yBone.RotateX(Mathf.DegToRad(-rotationAmount.Y / 2.75f));
                }
                else
                {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                }
            }
            if (Input.IsActionJustPressed("ZO") || Input.IsActionPressed("ZO1"))
            {
                zoom += 0.5f + (Input.IsActionJustPressed("ZO") ? 1f : 0f);
                Plm.Visible = true;
                if (zoom >= max_zoom)
                {
                    zoom = max_zoom;
                }
            }
            if (Input.IsActionJustPressed("ZI") || Input.IsActionPressed("ZI1"))
            {
                zoom -= 0.5f + (Input.IsActionJustPressed("ZI") ? 1f : 0f);
                if (zoom <= min_zoom)
                {
                    zoom = min_zoom;
                    Plm.Visible = false;
                }
            }
        }
    }
    public bool isStandingStill(Vector3 vel)
    {
        if ((Math.Abs(vel.X - Velocity.X)+Math.Abs(vel.Y - Velocity.Y)+Math.Abs(vel.Z - Velocity.Z)) > 0f)
            return false;
        return true;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!isClient)
        {
            Position = Position.Lerp(pos, 0.5f);
            Rotation = Rotation.Lerp(rot, 0.5f);
			if (UI != null) {
                UI.QueueFree();
                UI = null;
            }
        } else {
            Vector3 velocity = Velocity;
            Vector3 sigma = Camera.Position;
			sigma.Z = Mathf.Lerp(Camera.Position.Z, zoom, 0.15f); 
			Camera.Position = sigma;
            if (FocusTester)
            {
                if (Input.IsActionPressed("Jump"))
                {
                    if (IsOnFloor())
                    {
                        velocity.Y = JumpVelocity * 1.5f;
                    }
                }
            }
            if (!IsOnFloor())
            {
            	velocity += GetGravity() * (float)delta * 3.0f;
            }
            Vector2 inputDir = Input.GetVector("A", "D", "W", "S");
            Vector3 forwardDir = xBone.GlobalTransform.Basis.Z;
			forwardDir.Y = 0; 
			forwardDir = forwardDir.Normalized();
			
			Vector3 rightDir = xBone.GlobalTransform.Basis.X;
			rightDir.Y = 0;
			rightDir = rightDir.Normalized();

			Vector3 direction = (forwardDir * inputDir.Y + rightDir * inputDir.X).Normalized();
            if (FocusTester)
            {
                velocity.X += direction.X * Speed;
                velocity.Z += direction.Z * Speed;
            }
            if (direction.Length() > 0.01f)
            {
                Vector3 flatDir = new Vector3(direction.X, 0, direction.Z).Normalized();
                Transform3D t = GlobalTransform;
                t.Basis = Basis.LookingAt(flatDir, Vector3.Up);
                if (FocusTester)
                    Plm.GlobalTransform = t;
            }
            if (zoom == min_zoom)
            {
                Plm.GlobalRotation = xBone.Rotation;
            }
            velocity.X *= 0.75f;
			velocity.Z *= 0.75f;
			Velocity = velocity;
            MoveAndSlide();
        }
    }
	public void FE() {
        FocusTester = true;
    }
	public void FEX() {
        FocusTester = false;
    }
    public void Polling() {
        if (Client == null) return;
        Client.CallDeferred("UpdatedPosition");
    }
	public void TextSubmitted(string new_text) {
		if (Client == null) return;
        Text.Text = "";
        Client.CallDeferred("SendMessage", new_text);
    }
	public void AddPlayerLeaderBoard(string username) {
        Node LBN = LeaderboardName.Instantiate();
        LBN.GetNode<Label>("Label").Text = username;
        LBN.Name = username;
        LeaderboardList.AddChild(LBN);
    }
	public void RemovePlayerLeaderBoard(string username) {
        LeaderboardList.GetNode<Panel>(username).QueueFree();
    }
}
