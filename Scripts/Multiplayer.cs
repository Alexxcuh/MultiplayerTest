using Godot;
using Websocket.Client;
using System;
using MPTest.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
public partial class Multiplayer : Control
{
    [Export] private LineEdit Address;
    [Export] private CanvasLayer MenuUI;
    [Export] private Label Warning;
    [Export] private PackedScene World;
    [Export] private PackedScene Player;
    private Godot.Collections.Dictionary<long, GamePlayer> players = new();
    private string IPString;
    private Node LocalWorld;
    private GamePlayer LocalPlayer;
    public WebsocketClient ws;
    public long UID;
    public void UpdatedPosition()
    {
        if (ws == null) return;

        var packet = new MovementPacket
        {
            type = "movement",
            id = UID,
            position = new PositionMovement
            {
                x = LocalPlayer.Position.X,
                y = LocalPlayer.Position.Y,
                z = LocalPlayer.Position.Z
            },
            rotation = new RotationMovement
            {
                x = LocalPlayer.Plm.Rotation.X,
                y = LocalPlayer.Plm.Rotation.Y,
                z = LocalPlayer.Plm.Rotation.Z
            }
        };

        ws.Send(JsonSerializer.Serialize(packet));
    }

    private async void _on_button_pressed() {
        if (ws != null && ws.IsRunning) { 
            Warning.Text = "You're already in the server.";
            return;
        }
        ws = new WebsocketClient(new Uri($"ws://{Address.Text}"));
        ws.MessageReceived.Subscribe(msg =>
        {
            string json = msg.Text;
            if ( msg == null || json == null ) {
                Warning.Text = "the data provided by the server was null.";
                return;
            };
            IdentifierPacket ident = JsonSerializer.Deserialize<IdentifierPacket>(json);
            switch (ident.type)
            {
                case "movement":
                    MovementPacket Movement = JsonSerializer.Deserialize<MovementPacket>(json);
                    if (Movement.id == UID) break;
                    PositionMovement Pos = Movement.position;
                    RotationMovement Rot = Movement.rotation;
                    if (players.TryGetValue(Movement.id, out var b))
                    {
                        b.rot = new Vector3(Rot.x, Rot.y, Rot.z);
                        b.pos = new Vector3(Pos.x, Pos.y, Pos.z);
                    }
                    else
                    {
                        GD.Print("hmmm");
                        var e = Player.Instantiate<GamePlayer>();
                        e.Name = Movement.id.ToString();
                        LocalWorld.CallDeferred("add_child", e);
                        e.Label.Text = e.Name;
                        players[Movement.id] = e;
                        e.rot = new Vector3(Rot.x, Rot.y, Rot.z);
                        e.pos = new Vector3(Pos.x, Pos.y, Pos.z);
                        LocalPlayer.CallDeferred("AddPlayerLeaderBoard", e.Name);
                        e.Plm.Visible = true;
                    }
                    break;
                case "chat":
                    ChatPacket Message = JsonSerializer.Deserialize<ChatPacket>(json);
                    LocalPlayer.Chat.CallDeferred("set_text", LocalPlayer.Chat.Text + $"{Message.username}: {Message.message}\n");
                    break;
                case "init":
                    InitializePacket Init = JsonSerializer.Deserialize<InitializePacket>(json);
                    UID = Init.id;
                    CallDeferred(nameof(InitializePlayer));
                    if (Init.playersdata.Count == 0) break;
                    for (int i = 0; i < Init.playersdata.Count; i++)
                    {
                        Vec3 pos = Init.playersdata.GetValueOrDefault(i.ToString())[0];
                        Vec3 rot = Init.playersdata.GetValueOrDefault(i.ToString())[1];
                        var e = Player.Instantiate<GamePlayer>();
                        e.Name = i.ToString();
                        LocalWorld.CallDeferred("add_child", e);
                        e.Label.Text = e.Name;
                        players[i] = e;
                        e.rot = new Vector3(rot.x, rot.y, rot.z);
                        e.pos = new Vector3(pos.x, pos.y, pos.z);
                        LocalPlayer.CallDeferred("AddPlayerLeaderBoard",e.Name);
                    }
                    break;
                case "leave":
                    LeavePacket left = JsonSerializer.Deserialize<LeavePacket>(json);
                    if (players.TryGetValue(left.id, out var c))
                    {
                        c.QueueFree();
                        players.Remove(left.id);
                        LocalPlayer.CallDeferred("RemovePlayerLeaderBoard",left.id.ToString());
                    }
                    break;
                default: break;

            }
        });
        await ws.Start();
        if (!ws.IsRunning)
        {
            Warning.Text = $"Failed to connect, make sure the server exists / the IP is correct";
        };
    }
    public void InitializePlayer()
    {
        LocalWorld = World.Instantiate();
        LocalPlayer = Player.Instantiate<GamePlayer>();
        LocalPlayer.Position = new Vector3(0,5,0);
        LocalPlayer.Name = UID.ToString();
        LocalPlayer.Label.Text = LocalPlayer.Name;
        if (MenuUI != null && IsInstanceValid(MenuUI))
        {
            MenuUI.QueueFree();
            MenuUI = null;
        }
        AddChild(LocalWorld);
        LocalWorld.AddChild(LocalPlayer);
        LocalPlayer.Camera.Current = true;
        LocalPlayer.isClient = true;
        LocalPlayer.Client = this;
        LocalPlayer.Label.Visible = false;
        LocalPlayer.CallDeferred("AddPlayerLeaderBoard",LocalPlayer.Name);
    }
    public void SendMessage(string text){
        ws.Send(JsonSerializer.Serialize(new ChatPacket
        {
            type = "chat",
            id = UID,
            username = UID.ToString(),
            message = text
        }));
    }
}