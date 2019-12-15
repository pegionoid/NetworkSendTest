using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPSender : MonoBehaviour
{
    public string clientIp;
    public int port;
    private UdpClient udpclient;
    // Start is called before the first frame update

    void Start()
    {
        udpclient = new UdpClient();
        udpclient.Connect(clientIp, port);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        RenderTexture.active = source;
        tex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.Destroy(tex);

        udpclient.Send(bytes, bytes.Length);

        Graphics.Blit(source, destination);

        //byte[] bytes = System.Text.Encoding.GetEncoding("shift_jis").GetBytes("Test");
        //udpclient.Send(bytes, bytes.Length);


    }

    void OnApplicationQuit()
    {
        byte[] bytes = System.Text.Encoding.GetEncoding("shift_jis").GetBytes("END");
        udpclient.Send(bytes, bytes.Length);

        udpclient.Close();
    }
}
