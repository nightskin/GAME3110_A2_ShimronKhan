using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands
    {
        PLAYER_UPDATE,
        SERVER_UPDATE,
        HANDSHAKE,
        DISCONNECT
    }
    

    [System.Serializable]
    public class NetworkMessage
    {
        public Commands cmd;
    }

    [System.Serializable]
    public class HandshakeMsg : NetworkMessage
    {
        public NetworkPlayer player;
        public HandshakeMsg()
        {      // Constructor
            player = new NetworkPlayer();
            cmd = Commands.HANDSHAKE;
        }
    }

    [System.Serializable]
    public class PlayerUpdateMsg : NetworkMessage
    {
        public NetworkPlayer player;
        public PlayerUpdateMsg()
        {      // Constructor
            player = new NetworkPlayer();
            cmd = Commands.PLAYER_UPDATE;
        }
    };
    
    public class DisconnectMsg : NetworkMessage
    {
        public NetworkPlayer player;
        public DisconnectMsg()
        {
            player = new NetworkPlayer();
            cmd = Commands.DISCONNECT;
        }
    }

    [System.Serializable]
    public class ServerUpdateMsg : NetworkMessage
    {
        public List<NetworkPlayer> players = new List<NetworkPlayer>();
        public enum UpdateType
        {
            ADD,
            REMOVE,
            UPDATE_POS
        }
        public UpdateType type;
        public ServerUpdateMsg()
        {
            cmd = Commands.SERVER_UPDATE;
        }
    }
}



[System.Serializable]
public class NetworkPlayer
{
    public string id;
    public Vector3 pos;
    public bool destroy;
    public NetworkPlayer()
    {
        destroy = false;
    }

}


