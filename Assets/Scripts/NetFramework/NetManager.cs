using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using ProtoBuf;
using UnityEngine;
using PBMessage;

public static partial class NetManager
{
    public enum ServerType
    {
        Gateway,//网关服务器
        Fighter,//战斗服务器
    }
    
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
    private static List<IExtensible> _msgList;

    /// <summary>
    /// 是否正在连接
    /// </summary>
    private static bool _isConnecting;

    /// <summary>
    /// 是否正在关闭
    /// </summary>
    private static bool _isClosing;

    /// <summary>
    /// 发送队列
    /// </summary>
    private static Queue<ByteArray> _writeQueue;

    /// <summary>
    /// 一帧处理的最大消息量
    /// </summary>
    private static readonly int MaxProcessMsgCount = 10;

    // /// <summary>
    // /// 是否启用心跳机制
    // /// </summary>
    // private static bool _isUsePing = true;
    //
    // /// <summary>
    // /// 上一次发送Ping的时间
    // /// </summary>
    // private static float _lastPingTime = 0;
    //
    // /// <summary>
    // /// 上一次收到Point的时间
    // /// </summary>
    // private static float _lastPongTime = 0;
    //
    // /// <summary>
    // /// 心跳机制的时间间隔
    // /// </summary>
    // private static float _pingInterval = 2;
    
    /// <summary>
    /// 初始化
    /// </summary>
    private static void Init()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _byteArray = new ByteArray();
        _msgList = new List<IExtensible>();
        _writeQueue = new Queue<ByteArray>();
        _isConnecting = false;
        _isClosing = false;

        // _lastPingTime = Time.time;
        // _lastPongTime = Time.time;
        //
        // if (!_msgListeners.ContainsKey("MsgPong"))
        // {
        //     AddMsgListener("MsgPong", OnMsgPong);
        // }
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="ip">ip地址</param>
    /// <param name="port">端口号</param>
    public static void Connect(string ip, int port)
    {
        if (_socket != null && _socket.Connected)
        {
            Debug.LogError("连接失败，已经连接过了");
            return;
        }
        if (_isConnecting)
        {
            Debug.LogError("连接失败，正在连接中");
            return;
        }
        Init();
        _isConnecting = true;
        _socket?.BeginConnect(ip, port, ConnectCallback, _socket);
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
            
            DispatchEvent(NetEvent.ConnectSuccess, "");

            _isConnecting = false;
            
            //接收消息
            socket.BeginReceive(_byteArray.bytes, _byteArray.writeIndex, _byteArray.Remain, SocketFlags.None,
                ReceiveCallback, socket);
        }
        catch (SocketException e)
        {
            Debug.LogError("连接失败" + e.Message);
            DispatchEvent(NetEvent.ConnectFail, e.Message);
            _isConnecting = false;
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

    /// <summary>
    /// 关闭客户端
    /// </summary>
    private static void Close()
    {
        if (_socket == null || !_socket.Connected)
            return;
        if (_isConnecting)
            return;
        
        //消息还没有发送完
        if (_writeQueue.Count > 0)
        {
            _isClosing = true;
        }
        else
        {
            _socket.Close();
            DispatchEvent(NetEvent.Close, "");
        }
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
        int nameCount = 0;
        string protoName = ProtobufTool.DecodeName(_byteArray.bytes, _byteArray.readIndex, out nameCount);
        if (protoName == "")
        {
            Debug.LogError($"协议名解析失败");
            return;
        }
        _byteArray.readIndex += nameCount;

        //解析协议体
        int bodyLength = length - nameCount;
        IExtensible msgBase = ProtobufTool.Decode(protoName, _byteArray.bytes, _byteArray.readIndex, bodyLength);
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
    public static void Send(IExtensible msg, ServerType serverType)
    {
        if (_socket == null || !_socket.Connected) 
            return;
        if(_isConnecting)
            return;
        if (_isClosing)
            return;
        
        //编码
        byte[] nameBytes = ProtobufTool.EncodeName(msg);
        byte[] bodyBytes = ProtobufTool.Encode(msg);
        int len = nameBytes.Length + bodyBytes.Length + 1;
        byte[] sendBytes = new byte[len + 2];
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);
        sendBytes[2] = (byte)serverType;
        Array.Copy(nameBytes, 0, sendBytes, 3, nameBytes.Length);
        Array.Copy(bodyBytes, 0, sendBytes, 3 + nameBytes.Length, bodyBytes.Length);

        ByteArray ba = new ByteArray(sendBytes);
        int count = 0;
        lock (_writeQueue)
        {
            _writeQueue.Enqueue(ba);
            count = _writeQueue.Count;
        }

        if (count == 1)
        {
            Debug.Log($"发送消息：{msg.ToString()} {serverType}");
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

        //没有发送完成，还有消息要继续发送
        if (ba != null)
        {
            _socket.BeginSend(ba.bytes, ba.readIndex, ba.Length, SocketFlags.None, SendCallback, _socket);
        }

        if (_isClosing)
        {
            _socket.Close();
        }
    }

    private static void MsgUpdate()
    {
        //没有消息
        if(_msgList.Count==0)
            return;

        for (int i = 0; i < MaxProcessMsgCount; i++)
        {
            IExtensible msgBase = null;
            lock (_msgList)
            {
                if (_msgList.Count > 0)
                {
                    msgBase = _msgList[0];
                    _msgList.RemoveAt(0);
                }
            }

            if (msgBase != null)
            {
                PropertyInfo info = msgBase.GetType().GetProperty("protoName");
                string protoName = info.GetValue(msgBase).ToString();
                DispatchMsg(protoName, msgBase);
            }
            else
            {
                break;
            }
        }
    }

    // private static void PingUpdate()
    // {
    //     if(!_isUsePing)
    //         return;
    //
    //     if (Time.time - _lastPingTime > _pingInterval)
    //     {
    //         //发送
    //         MsgPing msgPing = new MsgPing();
    //         Send(msgPing, ServerType.Gateway);
    //         _lastPingTime = Time.time;
    //     }
    //     
    //     //断开的处理
    //     if (Time.time - _lastPongTime > _pingInterval * 4)
    //     {
    //         Close();
    //     }
    // }

    public static void Update()
    {
        // PingUpdate();
        MsgUpdate();
    }

    // /// <summary>
    // /// 处理Pong
    // /// </summary>
    // /// <param name="msgBase"></param>
    // private static void OnMsgPong(IExtensible msgBase)
    // {
    //     _lastPongTime = Time.time;
    // }
}
