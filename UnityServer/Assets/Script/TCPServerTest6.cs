using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


public class TCPServerTest6 : MonoBehaviour
{
    [SerializeField] public string _ipaddress;
    [SerializeField] public int _port;
    [SerializeField] public int ClientHeartbeatInterval;

    private const int STARTFLG = 1;

    private TcpListener tcpListener;
    private DisplayClientManager displayClientManager;

    private UdpClient udpClient;

    private int framecount = 0;

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
        // 規定の間隔で全ディスプレイクライアントの生存監視を行う
        if (++framecount >= ClientHeartbeatInterval)
        {
            displayClientManager.Heartbeat();
            framecount = 0;
        }
    }

    // 終了処理
    protected virtual void OnApplicationQuit()
    {
        displayClientManager.close();
        if (tcpListener != null)
        {
            tcpListener.Stop();
        }
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
        Debug.Log("Connect: " + client.Client.RemoteEndPoint + Environment.NewLine
                                  + "ReceiveBufferSize: " + client.Client.ReceiveBufferSize + Environment.NewLine
                                  + "SendBufferSize: " + client.Client.SendBufferSize);

        // 接続が確立したら次の人を受け付ける
        listener.BeginAcceptSocket(DoAcceptTcpClientCallback, listener);

        // 今接続した人とのネットワークストリームを取得
        NetworkStream stream = client.GetStream();

        // 接続が切れるまで送受信を繰り返す
        while (client.Connected)
        {
            while (stream.DataAvailable)
            {
                byte[] vs = new byte[1024];
                // 一行分の文字列を受け取る
                int r = stream.Read(vs, 0, vs.Length);
                if (r >= 10)
                {
                    OnMessage(vs, client);
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
                    return;
                }
            }
        }
    }

    // メッセージ受信
    private void OnMessage(byte[] msg, TcpClient tcpClient)
    {
        // 0（UDP初期化処理）
        if (msg[0] == 0)
        {
            // メッセージ区分０(1byte)
            // ディスプレイID(1byte)
            // TCP受信ポート番号（2byte）
            // UDP受信ポート番号（2byte）
            // UDP受信バッファサイズ（4byte）
            int dispid = msg[1];
            string ip = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
            int tcpport = BitConverter.ToUInt16(msg, 2);
            int udpport = BitConverter.ToUInt16(msg, 4);
            int udpbufsize = BitConverter.ToInt32(msg, 6);

            Debug.Log("Received[" + tcpClient.Client.RemoteEndPoint + "] : " + string.Join(",", msg) + Environment.NewLine
                      + "dispid : " + dispid + Environment.NewLine
                      + "tcpport : " + tcpport + Environment.NewLine
                      + "udpport : " + udpport + Environment.NewLine
                      + "udpbufsize : " + udpbufsize);

            DisplayClient wkdispClient;
            if (displayClientManager.createDisplayClient(dispid, ip, tcpport, udpport, out wkdispClient))
            {
                if (wkdispClient.UdpClient.Client.SendBufferSize > udpbufsize)
                {
                    wkdispClient.UdpClient.Client.SendBufferSize = udpbufsize;
                }

                byte[] senddata = { 0 };
                byte[] wkbs = BitConverter.GetBytes(wkdispClient.UdpClient.Client.SendBufferSize);

                Array.Resize(ref senddata, senddata.Length + wkbs.Length);
                wkbs.CopyTo(senddata, 1);

                Debug.Log("udpbufsize : " + BitConverter.ToInt32(senddata, 1));
                try
                {
                    // TCPクライアントを接続
                    wkdispClient.TcpConnect();
                    // 同期送信
                    wkdispClient.TcpSend(senddata);
                    // TCPクライアントを切断
                    wkdispClient.TcpDisConnect();
                    Debug.Log("Send Data");
                }
                catch(Exception e)
                {
                    Debug.Log(e);
                    return;
                }

                displayClientManager.Add(wkdispClient);
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

    // 初期化　：TCPで同期送受信
    // 生存監視：TCPで同期送信、非同期受信、非同期時間待ち
    // 画像送信：UDPで非同期送信
    public class DisplayClientManager
    {
        private List<DisplayClient> displayClientList = new List<DisplayClient>();

        /// <summary>
        /// 現在有効なDisplayClientの数
        /// </summary>
        public int Count
        {
            get
            {
                return displayClientList.Count;
            }
        }

        /// <summary>
        /// 与えられたディスプレイクライアントを追加する
        /// </summary>
        /// <param name="displayClient"></param>
        /// <returns></returns>
        public Boolean Add(DisplayClient displayClient)
        {
            if (displayClientList.Exists(d => d.DispId == displayClient.DispId))
            {
                Debug.Log("dispID[" + displayClient.DispId + "] is allready exists.");
                return false;
            }

            displayClientList.Add(displayClient);
            return true;

        }

        /// <summary>
        /// 指定されたID、IP/Portのディスプレイクライアントを新規作成し、追加する
        /// </summary>
        /// <param name="dispID"></param>
        /// <param name="ip"></param>
        /// <param name="tcpPort"></param>
        /// <param name="udpPort"></param>
        /// <returns></returns>
        public Boolean Add(int dispID, String ip, int tcpPort, int udpPort)
        {
            if (displayClientList.Exists(d => d.DispId == dispID))
            {
                Debug.Log("dispID[" + dispID + "] is allready exists.");
                return false;
            }

            displayClientList.Add(new DisplayClient(dispID, ip, tcpPort, udpPort));
            Debug.Log("dispID[" + dispID + "] added.");
            return true;
        }

        /// <summary>
        /// 指定されたIDのディスプレイクライアントを削除する
        /// </summary>
        /// <param name="dispID"></param>
        /// <returns></returns>
        public Boolean Remove(int dispID)
        {
            DisplayClient dc = getDisplayClient(dispID);
            if (dc == null)
            {
                Debug.Log($"DispId({dispID}) is not exists.");
                return false;
            }
            displayClientList.Remove(dc);
            dc.Close();
            return true;
        }

        /// <summary>
        /// 指定されたIP/Portのディスプレイクライアントを削除する
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="udpport"></param>
        /// <returns></returns>
        public Boolean Remove(String ip, int udpport)
        {
            DisplayClient dc = getDisplayClient(ip, udpport);
            if (dc == null)
            {
                Debug.Log("[" + ip + ":" + udpport + "] is not exists.");
                return false;
            }
            dc.Close();
            return displayClientList.Remove(dc);
        }

        /// <summary>
        /// 新しいディスプレイクライアントを生成する
        /// </summary>
        /// <param name="dispID"></param>
        /// <param name="ip"></param>
        /// <param name="tcpPort"></param>
        /// <param name="udpPort"></param>
        /// <param name="displayClient"></param>
        /// <returns></returns>
        public bool createDisplayClient(int dispID, String ip, int tcpPort, int udpPort, out DisplayClient displayClient)
        {
            displayClient = null;
            if (displayClientList.Exists(d => d.DispId == dispID))
            {
                Debug.Log("dispID[" + dispID + "] is allready exists.");
                return false;
            }
            displayClient = new DisplayClient(dispID, ip, tcpPort, udpPort);
            return true;

        }

        /// <summary>
        /// 指定されたIDのディスプレイクライアントを取得する
        /// </summary>
        /// <param name="dispID"></param>
        /// <returns></returns>
        public DisplayClient getDisplayClient(int dispID)
        {
            return displayClientList.Find(d => d.DispId == dispID);
        }

        /// <summary>
        /// 指定されたIP/Portのディスプレイクライアントを取得する
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="udpport"></param>
        /// <returns></returns>
        public DisplayClient getDisplayClient(String ip, int udpport)
        {
            return displayClientList.Find(d => d.Ip == ip && d.UdpPort == udpport);
        }

        /// <summary>
        /// 全ディスプレイクライアントの生存監視
        /// </summary>
        public void Heartbeat()
        {
            //Debug.Log($"DisplayClientCount : {Count}");
            List<DisplayClient> dclist = new List<DisplayClient>(displayClientList);
            foreach (DisplayClient dc in dclist)
            {
                // ディスプレイクライアントからの返答が得られていない場合
                // 切断と判断し、マネージャから削除する
                if (dc.IsAlive == false)
                {
                    Debug.Log($"DispId({dc.DispId}) is not alive.");
                    Remove(dc.DispId);
                    continue;
                }
                // ディスプレイクライアントからの返答が得られている場合、生存監視を行う
                dc.Heartbeat();
            }
        }

        /// <summary>
        /// 全ディスプレイクライアントを終了する
        /// </summary>
        public void close()
        {
            foreach (DisplayClient dc in displayClientList)
            {
                dc.Close();
            }
            displayClientList.Clear();
        }
    }
    public class DisplayClient
    {
        public int DispId { get; }
        public bool IsAlive { get; private set; }

        private IPEndPoint TcpIpendpoint { get; set; }
        private Socket TcpSocket { get; set; }
        private const int BufferSize = 1;
        private byte[] Buffer = new byte[BufferSize];
        public UdpClient UdpClient { get; private set; }
        public String Ip
        {
            get
            {
                if (UdpClient == null) return null;
                else return ((IPEndPoint)UdpClient.Client.RemoteEndPoint).Address.ToString();
            }
        }
        public int TcpPort
        {
            get
            {
                if (TcpIpendpoint == null) return 0;
                else return TcpIpendpoint.Port;
            }
        }

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
            try
            {
                this.DispId = dispID;
                this.UdpClient = new UdpClient(ip, udpPort);

                IPAddress ipAddress = IPAddress.Parse(ip);
                this.TcpIpendpoint = new IPEndPoint(ipAddress, tcpPort);

                this.IsAlive = true;

            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }
        }

        /// <summary>
        /// ディスプレイクライアントの生存監視
        /// </summary>
        public void Heartbeat()
        {
            // 生存フラグを無効にする
            this.IsAlive = false;

            // ディスプレイクライアントに接続
            TcpConnect();
            // 非同期で受信を待機
            // 生存報告パケットを受信した場合、生存フラグが有効になる
            this.TcpSocket.BeginReceive(this.Buffer, 0, BufferSize, SocketFlags.None, new AsyncCallback(HeatbeatCallback), this.TcpSocket);

            // 生存監視パケットを送信
            byte[] senddata = { 1 };
            TcpSend(senddata);

        }

        /// <summary>
        /// ディスプレイクライアントへのTCP送信
        /// </summary>
        /// <param name="senddata"></param>
        public void TcpSend(byte[] senddata)
        {
            Debug.Log($"SendData to DispID({DispId}) : {string.Join(",", senddata)}");
            this.TcpSocket.Send(senddata);
        }

        /// <summary>
        /// ディスプレイクライアントへのTCP接続
        /// </summary>
        public void TcpConnect()
        {
            Debug.Log($"DispID({DispId}) connecting to [{TcpIpendpoint.Address.ToString()}:{TcpIpendpoint.Port}].");
            try
            {
                this.TcpSocket = new Socket(this.TcpIpendpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                this.TcpSocket.Connect(this.TcpIpendpoint);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                TcpDisConnect();
                throw;
            }

            Debug.Log($"DispID({DispId}) connected.");
        }

        /// <summary>
        /// ディスプレイクライアントへのTCP接続を切断
        /// </summary>
        public void TcpDisConnect()
        {
            // TCP接続がない場合、終了
            if (!TcpSocket.Connected) return;

            try
            {
                // TCP接続を再利用不能状態で切断し、破棄
                this.TcpSocket?.Disconnect(false);
                this.TcpSocket?.Dispose();
                Debug.Log($"DispID({DispId}) is Disconnected.");

            }
            catch (ObjectDisposedException e)
            {
                Debug.Log(e);
                return;
            }
        }

        /// <summary>
        /// ディスプレイクライアントへのUDP送信
        /// </summary>
        /// <param name="source"></param>
        public void SendDisplay(RenderTexture source)
        {
            //Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            //RenderTexture.active = source;
            //tex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            //tex.Apply();

            //// Encode texture into PNG
            //byte[] bytes = tex.EncodeToPNG();
            //UnityEngine.Object.Destroy(tex);

            //udpclient.Send(bytes, bytes.Length);
        }

        /// <summary>
        /// ディスプレイクライアントの切断
        /// </summary>
        public void Close()
        {
            // UDP通信を切断
            UdpClient.Close();

            // 念のためTCP通信を切断
            TcpDisConnect();
            // TCP通信を接続
            TcpConnect();
            // 終了通知パケットを送信
            byte[] senddata = { 2 };
            TcpSend(senddata);

            // TCP通信を切断
            TcpDisConnect();
        }

        /// <summary>
        /// 生存報告パケット受信イベント
        /// </summary>
        /// <param name="asyncResult"></param>
        private void HeatbeatCallback(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;

            int byteSize = -1;
            try
            {
                byteSize = socket.EndReceive(asyncResult);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return;
            }

            // 受信したデータがある場合、その内容を表示する
            // 再度非同期での受信を開始する
            if (byteSize > 0)
            {
                if (Buffer[0] == 1)
                {
                    Debug.Log($"DispId({this.DispId}) is alive.");
                    this.IsAlive = true;
                    this.TcpDisConnect();
                }
            }
        }
    }
}



