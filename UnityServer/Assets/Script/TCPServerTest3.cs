using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class TCPServerTest3 : MonoBehaviour
{
    [SerializeField] public string _ipaddress;
    [SerializeField] public int _port;

    private const int STARTFLG = 1;

    private TcpListener tcpListener;
    private DisplayClientManager displayClientManager;

    // Start is called before the first frame update
    void Start()
    {
        displayClientManager = new DisplayClientManager();
        // 指定したポートを開く
        Listen(_ipaddress, _port);
    }

    // Update is called once per frame
    void Update()
    {

    }

    // 終了処理
    protected virtual void OnApplicationQuit()
    {
        if (tcpListener == null)
        {
            return;
        }

        tcpListener.Stop();
    }

    // ソケット接続準備、待機
    private void Listen(string host, int port)
    {
        Debug.Log("ipAddress:" + host + " port:" + port);
        var ip = IPAddress.Parse(host);
        tcpListener = new TcpListener(ip, port);
        tcpListener.Start();
        tcpListener.BeginAcceptSocket(DoAcceptTcpClientCallback, tcpListener);
    }

    // クライアントからの接続処理
    private void DoAcceptTcpClientCallback(IAsyncResult ar)
    {
        var listener = (TcpListener)ar.AsyncState;
        TcpClient client = listener.EndAcceptTcpClient(ar);
        Debug.Log("Connect: " + client.Client.RemoteEndPoint);
        Debug.Log("ReceiveBufferSize: " + client.Client.ReceiveBufferSize);
        Debug.Log("SendBufferSize: " + client.Client.SendBufferSize);

        // 接続が確立したら次の人を受け付ける
        listener.BeginAcceptSocket(DoAcceptTcpClientCallback, listener);

        // 今接続した人とのネットワークストリームを取得
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);

        // 接続が切れるまで送受信を繰り返す
        while (client.Connected)
        {
            while (!reader.EndOfStream)
            {
                // 一行分の文字列を受け取る
                var str = reader.ReadLine();
                OnMessage(str, client);
            }

            // 1000μs待って、接続状態が保留中、読取可、切断の場合
            if (client.Client.Poll(1000, SelectMode.SelectRead))
            {
                // かつ、クライアントからの読取可能データ量がZEROの場合
                if (client.Client.Available == 0)
                {
                    // クライアントが終了状態と判断し、切断
                    Debug.Log("Disconnect: " + client.Client.RemoteEndPoint);
                    client.Close();
                    break;
                }
            }
        }
    }

    // メッセージ受信
    private void OnMessage(String msg, TcpClient tcpClient)
    {
        Debug.Log("Received[" + tcpClient.Client.RemoteEndPoint + "] : " + msg);

        // ここでUDPstreamを開く
        byte[] vs = Encoding.UTF8.GetBytes("H");
        NetworkStream stream = tcpClient.GetStream();
        stream.Write(vs, 0, vs.Length);

    }

    // メッセージ送信
    private void SendMessage(int dispID, String msg)
    {

    }

    // データ送信
    private void SendData(int dispID, byte[] data)
    {

    }

    public class DisplayClientManager
    {
        private List<DisplayClient> displayClientList = new List<DisplayClient>();

        public int Count
        {
            get
            {
                return displayClientList.Count;
            }
        }

        public Boolean Add(int dispID, String ip, int tcpPort, int udpPort)
        {
            if (displayClientList.Exists(d => d.DispId == dispID)) return false;

            displayClientList.Add(new DisplayClient(dispID, ip, tcpPort, udpPort));
            return true;
        }

        public Boolean Remove(int dispID)
        {
            if (!(displayClientList.Exists(d => d.DispId == dispID))) return false;

            displayClientList.Remove(displayClientList.Find(d => d.DispId == dispID));
            return true;
        }

        public Boolean Remove(String ip)
        {
            if (!(displayClientList.Exists(d => d.Ip == ip))) return false;

            displayClientList.Remove(displayClientList.Find(d => d.Ip == ip));
            return true;
        }

        public DisplayClient getDisplayClient(int dispID)
        {
            return displayClientList.Find(d => d.DispId == dispID);
        }
    }

    public class DisplayClient
    {
        public int DispId { get; }
        public String Ip { get; }
        public int TcpPort { get; }
        public int UdpPort { get; }

        public DisplayClient(int dispID, String ip, int tcpPort, int udpPort)
        {
            this.DispId = dispID;
            this.Ip = ip;
            this.TcpPort = tcpPort;
            this.UdpPort = udpPort;
        }
    }
}


