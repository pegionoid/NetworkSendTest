using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class TCPServerTest4 : MonoBehaviour
{
    [SerializeField] public string _ipaddress;
    [SerializeField] public int _port;

    private const int STARTFLG = 1;

    private TcpListener tcpListener;
    private DisplayClientManager displayClientManager;

    private UdpClient udpClient;

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
        NetworkStream stream = client.GetStream();

        // 接続が切れるまで送受信を繰り返す
        while (client.Connected)
        {
            int dispid = -1;
            while (stream.DataAvailable)
            {
                byte[] vs = new byte[10];
                // 一行分の文字列を受け取る
                int r = stream.Read(vs, 0, vs.Length);
                if (r >= 10)
                {
                    if(vs[0] == 0)
                    {
                        // メッセージ区分０(1byte)
                        // ディスプレイID(1byte)
                        // TCP受信ポート番号（2byte）
                        // UDP受信ポート番号（2byte）
                        // UDP受信バッファサイズ（4byte）
                        dispid = vs[1];
                        string ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                        int tcpport = BitConverter.ToUInt16(vs, 2);
                        int udpport = BitConverter.ToUInt16(vs, 4);
                        int udpbufsize = BitConverter.ToInt32(vs, 6);

                        Debug.Log("Received[" + client.Client.RemoteEndPoint + "] : " + string.Join(",", vs));
                        Debug.Log("dispid : " + dispid);
                        Debug.Log("tcpport : " + tcpport);
                        Debug.Log("udpport : " + udpport);
                        Debug.Log("udpbufsize : " + udpbufsize);

                        if (displayClientManager.Add(dispid, ip, tcpport, udpport))
                        {
                            UdpClient wkudpClient = displayClientManager.getDisplayClient(dispid).UdpClient;
                            if (wkudpClient.Client.SendBufferSize > udpbufsize)
                            {
                                wkudpClient.Client.SendBufferSize = udpbufsize;
                            }
                            byte[] senddata = { 0 };
                            byte[] wkbs = BitConverter.GetBytes(wkudpClient.Client.SendBufferSize);
                            Array.Resize(ref senddata, senddata.Length + wkbs.Length);
                            wkbs.CopyTo(senddata, 1);
                            Debug.Log("udpbufsize : " + BitConverter.ToInt32(senddata, 1));
                            stream.Write(senddata, 0, senddata.Length);
                            Debug.Log("Send Data");
                        }
                    }
                }
            }

            // 1000μs待って、接続状態が保留中、読取可、切断の場合
            if (client.Client.Poll(1000, SelectMode.SelectRead))
            {
                // かつ、クライアントからの読取可能データ量がZEROの場合
                if (client.Client.Available == 0)
                {
                    // クライアントが終了状態と判断し、切断
                    Debug.Log("Disconnect: " + client.Client.RemoteEndPoint);
                    Debug.Log("Remove : " + displayClientManager.Remove(dispid));
                    client.Close();
                    break;
                }
            }
        }
    }

    // メッセージ受信
    private void OnMessage(byte[] msg, TcpClient tcpClient)
    {
        displayClientManager.Add(  BitConverter.ToInt16(msg, 0)
                                 , tcpClient.Client.RemoteEndPoint.ToString()
                                 , BitConverter.ToInt16(msg, 2)
                                 , BitConverter.ToInt16(msg, 4));

        if(udpClient == null)
        {
            udpClient = new UdpClient();
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
            if (displayClientList.Exists(d => d.DispId == dispID))
            {
                Debug.Log("dispID[" + dispID +"] is allready exists.");
                return false;
            }

            displayClientList.Add(new DisplayClient(dispID, ip, tcpPort, udpPort));
            Debug.Log("dispID[" + dispID + "] added.");
            return true;
        }

        public Boolean Remove(int dispID)
        {
            if (!(displayClientList.Exists(d => d.DispId == dispID))) return false;

            displayClientList.Remove(displayClientList.Find(d => d.DispId == dispID));
            return true;
        }

        public Boolean Remove(String ip, int udpport)
        {
            if (!(displayClientList.Exists(d => d.Ip == ip && d.UdpPort == udpport)))
            {
                Debug.Log("[" + ip + ":" + udpport + "] is not exists.");
                return false;
            }

            return displayClientList.Remove(displayClientList.Find(d => d.Ip == ip && d.TcpPort == udpport));
        }

        public DisplayClient getDisplayClient(int dispID)
        {
            return displayClientList.Find(d => d.DispId == dispID);
        }
    }

    public class DisplayClient
    {
        public int DispId { get; }
        public UdpClient UdpClient { get; private set; }
        public String Ip {
            get
            {
                if (UdpClient == null) return null;
                else return ((IPEndPoint)UdpClient.Client.RemoteEndPoint).Address.ToString();
            }
        }
        public int TcpPort{ get; }
        public int UdpPort
        {
            get
            {
                if (UdpClient == null) return 0;
                else return ((IPEndPoint)UdpClient.Client.RemoteEndPoint).Port;
            }
        }

        public DisplayClient(int dispID, String ip, int tcpPort, int udpPort)
        {
            this.DispId = dispID;
            this.UdpClient = new UdpClient(ip, udpPort);
            this.TcpPort = tcpPort;
        }
    }
}


