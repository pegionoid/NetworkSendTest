using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/*
 * SocketServer.cs
 * ソケット通信（サーバ）
 * Unityアプリ内にサーバを立ててメッセージの送受信を行う
 */
namespace Script.SocketServer
{
    public class SocketServer : MonoBehaviour
    {
    //    private TcpListener _listener;
    //    protected readonly DisplayClientManager _displayClientManager = new DisplayClientManager();

    //    // ソケット接続準備、待機
    //    protected void Listen(string host, int port)
    //    {
    //        Debug.Log("ipAddress:" + host + " port:" + port);
    //        var ip = IPAddress.Parse(host);
    //        _listener = new TcpListener(ip, port);
    //        _listener.Start();
    //        _listener.BeginAcceptSocket(DoAcceptTcpClientCallback, _listener);
    //    }

    //    // クライアントからの接続処理
    //    private void DoAcceptTcpClientCallback(IAsyncResult ar)
    //    {
    //        var listener = (TcpListener)ar.AsyncState;
    //        var client = listener.EndAcceptTcpClient(ar);
    //        Debug.Log("Connect: " + client.Client.RemoteEndPoint);

    //        // 接続が確立したら次の人を受け付ける
    //        listener.BeginAcceptSocket(DoAcceptTcpClientCallback, listener);

    //        // 今接続した人とのネットワークストリームを取得
    //        var stream = client.GetStream();
    //        var reader = new StreamReader(stream, Encoding.UTF8);

    //        // 接続が切れるまで送受信を繰り返す
    //        while (client.Connected)
    //        {
    //            while (!reader.EndOfStream)
    //            {
    //                // 一行分の文字列を受け取る
    //                var str = reader.ReadLine();
    //                OnMessage(str, client);
    //            }

    //            // クライアントの接続が切れたら
    //            if (client.Client.Poll(1000, SelectMode.SelectRead) && (client.Client.Available == 0))
    //            {
    //                Debug.Log("Disconnect: " + client.Client.RemoteEndPoint);
    //                client.Close();
    //                _clients.Remove(client);
    //                break;
    //            }
    //        }
    //    }
        

    //    // メッセージ受信
    //    protected virtual void OnMessage(string msg, TcpClient client)
    //    {
    //        Debug.Log(msg);

    //        if (msg.StartsWith("START"))
    //        {
    //            int dispID;
    //            if (int.TryParse(msg.Trim("START".ToCharArray()), out dispID))
    //            {
    //                _displayClientManager.Add(dispID, client);
    //            }
    //        }
    //    }

    //    // クライアントにメッセージ送信
    //    protected void SendMessageToClient(int destination, string msg)
    //    {
    //        if (_clients.Count == 0)
    //        {
    //            return;
    //        }
    //        var body = Encoding.UTF8.GetBytes(msg);
            
    //    }

    //    // 終了処理
    //    protected virtual void OnApplicationQuit()
    //    {
    //        if (_listener == null)
    //        {
    //            return;
    //        }

    //        if (_clients.Count != 0)
    //        {
    //            foreach (var client in _clients)
    //            {
    //                client.Close();
    //            }
    //        }
    //        _listener.Stop();
    //    }
    //}

    //public class DisplayClientManager
    //{
    //    private List<DisplayClient> _clients = new List<DisplayClient>();
        
    //    public TcpClient GetClient(int dispID)
    //    {
    //        return _clients.Find(d => d.DispID == dispID).Client;
    //    }

    //    public void Add(int dispID, TcpClient dispClient)
    //    {
    //        if(_clients.Exists(d => d.DispID == dispID))
    //        {
    //            throw new Exception("This ID already exists : " + dispID);
    //        }
    //        _clients.Add(new DisplayClient(dispID, dispClient));
    //    }

    //    public void Remove(int dispID)
    //    {
    //        if (!(_clients.Exists(d => d.DispID == dispID)))
    //        {
    //            throw new Exception("This ID not exists : " + dispID);
    //        }
    //        DisplayClient dc = _clients.Find(d => d.DispID == dispID);
    //        dc.Close();
    //        _clients.Remove(dc);
    //    }
    }

    public class DisplayClient
    {
        private int _dispID;
        private TcpClient _client;

        public int DispID { get { return _dispID; } }
        public TcpClient Client { get { return _client; } }

        public DisplayClient(int dispID, TcpClient client)
        {
            this._dispID = dispID;
            this._client = client;
        }

        public void Close()
        {
            Client.Close();
        }
    }
}