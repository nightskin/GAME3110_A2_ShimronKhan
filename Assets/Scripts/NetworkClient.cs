using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System.Text;
using System.Collections.Generic;

public class NetworkClient : MonoBehaviour
{
    public string serverIP;
    public ushort serverPort;
    //public UI_Handler console;
    public Material mat;

    private NetworkDriver m_Driver;
    NetworkConnection m_Connection;
    NetworkPlayer clientPlayer = new NetworkPlayer();
    List<NetworkPlayer> allPlayers = new List<NetworkPlayer>();

    ///-----------------Functions to make things easier----------------------------------------------///

    // Destroys a cube object from a network player
    void DestroyPlayerCube(NetworkPlayer player)
    {
        if (FindPlayerCube(player) != null)
        {
            Destroy(FindPlayerCube(player));
        }
    }

    // Spawns a cube object from a network player
    void SpawnPlayerCube(NetworkPlayer player)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.GetComponent<Renderer>().material = mat;
        obj.AddComponent<PlayerInfo>();
        obj.GetComponent<PlayerInfo>().ID = player.id;
        if (player.id == clientPlayer.id)
        {
            obj.AddComponent<Movement>();
        }
        obj.name = "Player: " + player.id;
        obj.transform.position = player.pos;
        if (FindPlayerCube(player) == null)
        {
            Instantiate(obj);
        }
    }

    // Find a cube object from a id
    GameObject FindPlayerCube(NetworkPlayer player)
    {
        string name = "Player: " + player.id;
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            return obj;
        }
        return null;
    }

    // used to send messages to the server
    void SendToServer(string message)
    {
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    // Creates a Vector and populates it with random values
    Vector3 RandomPos(float r)
    {
        float x = Random.Range(-r, r);
        float z = Random.Range(-r, r);
        return new Vector3(x, 0, z);
    }

    ///-------------------Client Player Functions----------------------//

    // Sends the client player to server to be included
    void ConnectClientPlayer(HandshakeMsg msg)
    {
        clientPlayer.id = msg.player.id;
        clientPlayer.pos = RandomPos(5);

        HandshakeMsg handShake = new HandshakeMsg();
        handShake.player = clientPlayer;
        SendToServer(JsonUtility.ToJson(handShake));
    }

    // Sends the clent player to server to be set to be destroyed
    public void DisconnectClientPlayer()
    {
        DisconnectMsg disconnect = new DisconnectMsg();
        disconnect.player.id = clientPlayer.id;
        SendToServer(JsonUtility.ToJson(disconnect));
    }

    // Sends curent player cube position to server
    void UpdateClientPlayer()
    {
        PlayerUpdateMsg playerUpdate = new PlayerUpdateMsg();
        playerUpdate.player.id = clientPlayer.id;
        playerUpdate.player.pos = FindPlayerCube(clientPlayer).transform.position;
        SendToServer(JsonUtility.ToJson(playerUpdate));
    }

    ///----------------------Object Functions------------------------//
    void AddObj(ServerUpdateMsg msg)
    {
        allPlayers = msg.players;
        foreach (NetworkPlayer p in allPlayers)
        {
            if (FindPlayerCube(p) == null)
            {
                if (!p.destroy)
                {
                    SpawnPlayerCube(p);
                }
            }
        }
    }

    void RemoveObj(ServerUpdateMsg msg)
    {
        allPlayers = msg.players;
        foreach (NetworkPlayer p in allPlayers)
        {
            if (FindPlayerCube(p) != null)
            {
                if (p.destroy)
                {
                    DestroyPlayerCube(p);
                }
            }
        }
    }

    void UpdateObjs(ServerUpdateMsg msg)
    {
        for (int p = 0; p < allPlayers.Count; p++)
        {
            for (int s = 0; s < msg.players.Count; s++)
            {
                if (allPlayers[p].id != clientPlayer.id &&  allPlayers[p].destroy == false)
                {
                    if (allPlayers[p].id == msg.players[s].id)
                    {
                        FindPlayerCube(allPlayers[p]).transform.position = msg.players[s].pos;
                    }
                }
            }
        }
    }

    ///-----------------------------Events-----------------------------------//

    // fired on start of client program
    void Start()
    {
        //console = GameObject.Find("UI").GetComponent<UI_Handler>();
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP, serverPort);
        m_Connection = m_Driver.Connect(endpoint);
    }

    // fired when connected to server
    void OnConnect()
    {

    }

    //  fired when data is recieved from server
    void OnData(DataStreamReader stream)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkMessage header = JsonUtility.FromJson<NetworkMessage>(recMsg);
        switch (header.cmd)
        {
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                ConnectClientPlayer(hsMsg);
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                if(suMsg.type == ServerUpdateMsg.UpdateType.ADD)
                {
                    AddObj(suMsg);
                    Debug.Log(JsonUtility.ToJson(suMsg));
                }
                else if(suMsg.type == ServerUpdateMsg.UpdateType.REMOVE)
                {
                    RemoveObj(suMsg);
                }
                else if(suMsg.type == ServerUpdateMsg.UpdateType.UPDATE_POS)
                {
                    UpdateObjs(suMsg);
                }
                break;
            default:
                Debug.Log("Unspecified Message type");
                break;
        }
    }

    // fired constantly until client program ends
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();
        //if connection has not been created
        if (!m_Connection.IsCreated)
        {
            return;
        }
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }
            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }

        if (FindPlayerCube(clientPlayer) != null)
        {
            UpdateClientPlayer();
        }
    }

    // fired before disconnection
    void OnDisconnect()
    {
        Disconnect();
    }

    // fired when this script is destroyed
    void OnApplicationQuit()
    {
        DisconnectClientPlayer();
        Disconnect();
    }

    // disconnects client
    void Disconnect()
    {
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

}