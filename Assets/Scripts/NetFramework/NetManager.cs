using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

public static class NetManager
{
    /// <summary>
    /// 客户端套接字
    /// </summary>
    private static Socket _socket;

    /// <summary>
    /// 存放消息的字节数组
    /// </summary>
    private static ByteArray _byteArray;

    /// <summary>
    /// 消息列表
    /// </summary>
    private static List<MsgBase> _msgList;

    /// <summary>
    /// 发送队列
    /// </summary>
    private static Queue<ByteArray> _writeQueue;
    
    /// <summary>
    /// 初始化
    /// </summary>
    private static void Init()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _byteArray = new ByteArray();
        _msgList = new List<MsgBase>();
        _writeQueue = new Queue<ByteArray>();
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

    private static void Close()
    {
        
    }

    /// <summary>
    /// 处理接收过来的消息
    /// </summary>
    private static void OnReceiveData()
    {
        if (_byteArray.Length <= 2) return;
        byte[] bytes = _byteArray.bytes;
        int readIndex = _byteArray.readIndex;
        //解析消息总体长度
        short length = (short)(bytes[readIndex + 1] * 256 + bytes[readIndex]);

        if (_byteArray.Length < length + 2) return;
        _byteArray.readIndex += 2;
        string protoName = MsgBase.DecodeName(_byteArray.bytes, _byteArray.readIndex, out var nameCount);
        if (protoName == "")
        {
            Debug.LogError($"协议名解析失败");
            return;
        }
        _byteArray.readIndex += nameCount;

        //解析协议体
        int bodyLength = length - nameCount;
        MsgBase msgBase = MsgBase.Decode(protoName, _byteArray.bytes, _byteArray.readIndex, bodyLength);
        _byteArray.readIndex += bodyLength;
        
        //移动数据
        _byteArray.MoveBytes();
        lock (_msgList)
        {
            _msgList.Add(msgBase);
        }

        if (_byteArray.Length > 2)
        {
            OnReceiveData();
        }
    }

    /// <summary>
    /// 发送协议
    /// </summary>
    /// <param name="msg"></param>
    public static void Send(MsgBase msg)
    {
        if (_socket == null || !_socket.Connected) 
            return;
        
        //编码
        byte[] nameBytes = MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);
        int len = nameBytes.Length + bodyBytes.Length;
        byte[] sendBytes = new byte[len + 2];
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);
        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);

        ByteArray ba = new ByteArray(sendBytes);
        int count = 0;
        lock (_writeQueue)
        {
            _writeQueue.Enqueue(ba);
            count = _writeQueue.Count;
        }

        if (count == 1)
        {
            _socket.BeginSend(sendBytes, 0, sendBytes.Length, SocketFlags.None, SendCallback, _socket);
        }
    }

    /// <summary>
    /// 发送回调
    /// </summary>
    /// <param name="ar"></param>
    private static void SendCallback(IAsyncResult ar)
    {
        Socket socket = (Socket)ar.AsyncState;
        if (socket == null || !socket.Connected)
            return;
        
        int count = socket.EndSend(ar);
        ByteArray ba;
        lock (_writeQueue)
        {
            ba = _writeQueue.First();
        }

        ba.readIndex += count;
        //如果这个byteArray已经发送完成
        if (ba.Length == 0)
        {
            lock (_writeQueue)
            {
                //清除
                _writeQueue.Dequeue();
                //取到下一个
                ba = _writeQueue.First();
            }
        }

        if (ba != null)
        {
            _socket.BeginSend(ba.bytes, ba.readIndex, ba.Length, SocketFlags.None, SendCallback, _socket);
        }
    }
}
