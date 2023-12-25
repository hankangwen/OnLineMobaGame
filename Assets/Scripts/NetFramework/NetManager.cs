using System;
using System.Net.Sockets;
using UnityEngine;

public static class NetManager
{
    /// <summary>
    /// 客户端套接字
    /// </summary>
    private static Socket _socket;

    /// <summary>
    /// 字节数组
    /// </summary>
    private static ByteArray _byteArray;
    
    /// <summary>
    /// 初始化
    /// </summary>
    private static void Init()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _byteArray = new ByteArray();
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="ip">ip地址</param>
    /// <param name="port">端口号</param>
    public static void Connect(string ip, int port)
    {
        Init();
        _socket.BeginConnect(ip, port, ConnectCallback, _socket);
    }

    /// <summary>
    /// 连接回调
    /// </summary>
    /// <param name="ar"></param>
    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndConnect(ar);
            Debug.Log("Connect Success!");
            
            //接收消息
            socket.BeginReceive(_byteArray.bytes, _byteArray.writeIndex, _byteArray.Remain, SocketFlags.None,
                ReceiveCallback, socket);
        }
        catch (SocketException e)
        {
            Debug.LogError("连接失败" + e.Message);
        }
    }

    /// <summary>
    /// 接收回调
    /// </summary>
    /// <param name="ar"></param>
    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            //接收的数据量
            int count = socket.EndReceive(ar);
            //断开连接
            if (count == 0)
            {
                Close();
                return;
            }
            //接收数据
            _byteArray.writeIndex += count;
            
            //处理消息
            OnReceiveData();
            //如果长度过小，扩容
            if (_byteArray.Remain < 8)
            {
                _byteArray.MoveBytes();
                _byteArray.Resize(_byteArray.Length * 2);
            }

            _socket.BeginReceive(_byteArray.bytes, _byteArray.writeIndex, _byteArray.Remain, SocketFlags.None,
                ReceiveCallback, socket);
        }
        catch (SocketException e)
        {
            Console.WriteLine("接收失败" + e.Message);
        }
    }

    private static void OnReceiveData()
    {
        throw new NotImplementedException();
    }

    private static void Close()
    {
        throw new NotImplementedException();
    }
}
