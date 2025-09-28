const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });
const clients = new Map(); // client map for the ID lookout and shit 
const clientData = new Map();
class Vec3 {
  constructor(x, y, z) {
    this.x = x;
    this.y = y;
    this.z = z;
  }
}
function wrap(value, min, max) {
  return ((value - min) % (max - min + 1) + (max - min + 1)) % (max - min + 1) + min;
}
wss.on('listening', () => {
  console.log('WebSocket server is up and running on ws://127.0.0.1:8080');
});
wss.on('error', (err) => {
  console.error('WebSocket server error:', err);
});
wss.on('connection', (ws) => {
  let uid = wrap(Date.now() * Math.floor(Math.random()*1000000),1,99999999999)
  clients.set(uid, ws);
  clientData.set(uid, [
    new Vec3(0, 0, 0),
    new Vec3(0, 0, 0)
  ]);
  ws.send(JSON.stringify({
    type: "init",
    playersdata: clientData,
    id: uid
  }));
  broadcastMovement(uid);
  console.log(`Client #${uid} connected`);
  ws.on('message', (msg) => {
    let json = JSON.parse(msg);
    console.log(json);
    if (clients.get(json.id) != ws) { return; };
    switch(json.type) {
      case "movement":
        let pos = json.position;
        let ros = json.rotation;
        clientData.set(uid, [
            new Vec3(pos.x, pos.y, pos.z),
            new Vec3(ros.x, ros.y, ros.z)
        ]);
        console.log(pos)
        broadcastMovement(uid);
        break;
      case "chat":
        broadcastMessage(uid, json.username, json.message);
      default: break;
    }
  });
  ws.on('close', () => {
    console.log(`Client #${uid} disconnected`);
    clients.delete(uid);
    clientData.delete(uid);
    broadcastLeave(uid);
  });
});
function broadcastLeave(i) {
  clients.forEach((ws) => {
    if (ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify({
        type: "leave",
        id: i,
      }));
    }
  });
}
function broadcastMovement(i) {
  clients.forEach((ws) => {
    if (ws.readyState === WebSocket.OPEN) {
      let [pos, ros] = clientData.get(i);
      ws.send(JSON.stringify({
        type: "movement",
        id: i,
        position: pos,
        rotation: ros
      }));
    }
  });
}
function broadcastMessage(i, username, message) {
  clients.forEach((ws) => {
    if (ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify({
        type: "chat",
        id: i,
        username: username,
        message: message
      }));
    }
  });
}
