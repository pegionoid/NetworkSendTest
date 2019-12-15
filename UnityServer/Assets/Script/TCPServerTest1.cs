using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class TCPServerTest1 : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] public string _ipaddress;
    // ポート指定（他で使用していないもの、使用されていたら手元の環境によって変更）
    [SerializeField] public int _port;
#pragma warning restore 0649
    private TcpListener tcpListener;

    private void Start()
    {
        // 指定したポートを開く
        Listen(_ipaddress, _port);
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
        var client = listener.EndAcceptTcpClient(ar);
        Debug.Log("Connect: " + client.Client.RemoteEndPoint);

        // 接続が確立したら次の人を受け付ける
        listener.BeginAcceptSocket(DoAcceptTcpClientCallback, listener);

        if (client.Connected)
        {
            client.Close();
        }
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



}