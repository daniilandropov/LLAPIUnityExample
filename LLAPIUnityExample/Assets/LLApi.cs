using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LLApi : MonoBehaviour
{
    public Text text;
    public bool isServer = false;
    int reliableChannelID;
    int hostID;
    int connectionID;
    int socketPort = 24322;
    int maxConnections = 2;
    byte error;
    public bool isInLobby = false;
    public bool isInGame = false;
    public bool secondPlayerIsConnected = false;

    public void HostLobby()
    {
        isServer = true;
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.ReliableSequenced);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);
        Debug.Log("server opened!");
        text.text = "server opened!";
        isInLobby = true;
    }

    public void JoinLobby()
    {
        isServer = false;
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.ReliableSequenced);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, 0);
        Debug.Log("Client socket open!  connecting..");
        connectionID = NetworkTransport.Connect(hostID, "127.0.0.1", socketPort, 0, out error);
        print("client connect2host connectionID= " + connectionID);
        print("error?: " + error);

        text.text = "client connect2host connectionID= " + connectionID;
        isInLobby = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) HostLobby();
        if (Input.GetKeyDown(KeyCode.W)) JoinLobby();

        if (isInGame)
        {
            if (!isServer)
            {
                if (Input.GetKeyDown(KeyCode.R)) SendMovementDataToServer(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()));

                int recHostID;
                int recConnectionID;
                int recChannelID;
                byte[] recBuffer = new byte[1024];
                int bufferSize = 1024;
                int dataSize;
                NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);
                switch (recNetworkEvent)
                {
                    case NetworkEventType.ConnectEvent:
                        print("connection made to host: " + recHostID.ToString());
                        break;
                    case NetworkEventType.DataEvent:
                        byte[] newBuffer = new byte[dataSize];
                        for (int i = 0; i < dataSize; i++)
                        {
                            newBuffer[i] = recBuffer[i];
                        }
                        text.text = Encoding.ASCII.GetString(newBuffer);
                        break;
                    case NetworkEventType.DisconnectEvent:
                        print("d/c from host!");
                        break;

                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.R)) SendMovementDataToClient(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()));

                int recHostId;
                int recConnectionId;
                int recChannelId;
                int bufferSize = 1024;
                byte[] buffer = new byte[1024];
                int dataSize;
                byte error;

                NetworkEventType networkEvent = NetworkTransport.Receive(out recHostId, out recConnectionId, out recChannelId, buffer, bufferSize, out dataSize, out error);
                switch (networkEvent)
                {
                    case NetworkEventType.ConnectEvent:
                        secondPlayerIsConnected = true;
                        Debug.Log("Player " + recConnectionId.ToString() + " connected to: " + recHostId.ToString() + "! addding as connectionID for host"); //server id is 0, player is anything between 1 and 3 (max 4 players)
                        break;
                    case NetworkEventType.DataEvent:
                        byte[] newBuffer = new byte[dataSize];
                        for (int i = 0; i < dataSize; i++)
                        {
                            newBuffer[i] = buffer[i];
                        }
                        text.text = Encoding.ASCII.GetString(newBuffer);
                        break;
                    case NetworkEventType.DisconnectEvent:
                        secondPlayerIsConnected = false;
                        Debug.Log("Removed connection: " + recConnectionId.ToString() + "!");
                        break;
                }
            }


        }
        else if (isInLobby)
        {
            if (!isServer)
            {
                int recHostID;
                int recConnectionID;
                int recChannelID;
                byte[] recBuffer = new byte[1024];
                int bufferSize = 1024;
                int dataSize;
                NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);
                switch (recNetworkEvent)
                {
                    case NetworkEventType.ConnectEvent:
                        print("connection made to host: " + recHostID.ToString());
                        break;
                    case NetworkEventType.DataEvent:
                        print("game starting cuz host said so");
                        isInGame = true;
                        isInLobby = false;
                        text.text = "STARTED!";
                        //SceneManager.LoadScene("SampleScene");
                        break;
                    case NetworkEventType.DisconnectEvent:
                        print("d/c from host!");
                        break;

                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.E) && secondPlayerIsConnected)
                {
                    SendMovementDataToClient(new byte[1]);
                    isInGame = true;
                    isInLobby = false;
                    text.text = "STARTED!";
                }
                else
                {
                    int recHostId;
                    int recConnectionId;
                    int recChannelId;
                    int bufferSize = 1024;
                    byte[] buffer = new byte[1024];
                    int dataSize;
                    byte error;

                    NetworkEventType networkEvent = NetworkTransport.Receive(out recHostId, out recConnectionId, out recChannelId, buffer, bufferSize, out dataSize, out error);
                    switch (networkEvent)
                    {
                        case NetworkEventType.ConnectEvent:
                            secondPlayerIsConnected = true;
                            Debug.Log("Player " + recConnectionId.ToString() + " connected to: " + recHostId.ToString() + "! addding as connectionID for host");
                            text.text = "Player " + recConnectionId.ToString() + " connected to: " + recHostId.ToString() + "! addding as connectionID for host";
                            break;
                        case NetworkEventType.DataEvent:
                            break;
                        case NetworkEventType.DisconnectEvent:
                            secondPlayerIsConnected = false;
                            Debug.Log("Removed connection: " + recConnectionId.ToString() + "!");
                            text.text = "Removed connection: " + recConnectionId.ToString() + "!";
                            break;
                    }
                }
            }

        }
    }

    public void SendMovementDataToServer(byte[] data)
    {
        NetworkTransport.Send(hostID, connectionID, reliableChannelID, data, data.Length, out error);

    }
    public void SendMovementDataToClient(byte[] data)
    {
        NetworkTransport.Send(hostID, 1, reliableChannelID, data, data.Length, out error); 
    } 
}
