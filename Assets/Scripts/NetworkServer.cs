using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System.Text;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public ushort serverPort;
    private NativeList<NetworkConnection> m_connections;
    public List<NetworkPlayer> Allplayers = new List<NetworkPlayer>();

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();
        m_connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }
    
    void SendToClient(string message, NetworkConnection c)
    {
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
    
    void SendToAllClients(string message)
    {
        for(int c= 0; c < m_connections.Length; c++)
        {
            SendToClient(message, m_connections[c]);
        }
    }

    void OnConnect(NetworkConnection c)
    {
        m_connections.Add(c);
        HandshakeMsg handshake = new HandshakeMsg();
        handshake.player.id = c.InternalId.ToString();
        SendToClient(JsonUtility.ToJson(handshake), c);
        Debug.Log("New Client ID: " + handshake.player.id.ToString() + " Is Being Connected.");
    }

    void AddPlayer(HandshakeMsg msg)
    {
        Allplayers.Add(msg.player);

        ServerUpdateMsg ServerUpdate = new ServerUpdateMsg();
        ServerUpdate.players = Allplayers;
        ServerUpdate.type = ServerUpdateMsg.UpdateType.ADD;
        SendToAllClients(JsonUtility.ToJson(ServerUpdate));
    }

    void RemovePlayer(DisconnectMsg msg)
    {
        foreach (NetworkPlayer p in Allplayers)
        {
            if(p.id == msg.player.id)
            {
                p.destroy = true;
            }
        }
        ServerUpdateMsg ServerUpdate = new ServerUpdateMsg();
        ServerUpdate.players = Allplayers;
        ServerUpdate.type = ServerUpdateMsg.UpdateType.REMOVE;
        SendToAllClients(JsonUtility.ToJson(ServerUpdate));
    }

    void UpdatePlayer(PlayerUpdateMsg msg)
    {
        foreach(NetworkPlayer p in Allplayers)
        {
            if(p.id == msg.player.id)
            {
                p.pos = msg.player.pos;
            }
        }
        ServerUpdateMsg ServerUpdate = new ServerUpdateMsg();
        ServerUpdate.players = Allplayers;
        ServerUpdate.type = ServerUpdateMsg.UpdateType.UPDATE_POS;
        SendToAllClients(JsonUtility.ToJson(ServerUpdate));
    }

    void OnData(DataStreamReader stream, int i)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkMessage header = JsonUtility.FromJson<NetworkMessage>(recMsg);
        switch (header.cmd) 
        {
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                AddPlayer(hsMsg);
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                UpdatePlayer(puMsg);
                break;
            case Commands.DISCONNECT:
                DisconnectMsg dcMsg = JsonUtility.FromJson<DisconnectMsg>(recMsg);
                RemovePlayer(dcMsg);
                break;
            default:
            Debug.Log("SERVER ERROR: Unrecognized message received!");
            break;
        }
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_connections.Dispose();
    }

    void OnDisconnect(int i)
    {
        string msg = "Client: " + m_connections[i].InternalId.ToString() + " Disconnected";
        Debug.Log(msg);
        m_connections[i] = default(NetworkConnection);
    }
    
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();
        // CleanUpConnections
        for (int i = 0; i < m_connections.Length; i++)
        {
            if (!m_connections[i].IsCreated)
            {
                m_connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // AcceptNewConnections
        NetworkConnection nc = m_Driver.Accept();
        while (nc  != default(NetworkConnection))
        {            
            OnConnect(nc);
            // Check if there is another new connection
            nc = m_Driver.Accept();
        }

        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_connections.Length; i++)
        {
            Assert.IsTrue(m_connections[i].IsCreated);
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnDisconnect(i);
                }
                cmd = m_Driver.PopEventForConnection(m_connections[i], out stream);
            }
        }
    }
}