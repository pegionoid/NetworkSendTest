using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class TCPServerTest2 : MonoBehaviour
{
    [SerializeField] public string _ipaddress;
    [SerializeField] public int _port;

    private TcpListener tcpListener;

    // Start is called before the first frame update
    void Start()
    {
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
        var client = listener.EndAcceptTcpClient(ar);
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
                Debug.Log("Received[" + client.Client.RemoteEndPoint + "] : " + str );
            }

            // 1000μs待って、接続状態が保留中、読取可、切断の場合
            if(client.Client.Poll(1000, SelectMode.SelectRead))
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
}
