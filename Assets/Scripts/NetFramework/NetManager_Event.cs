using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;
using ProtoBuf;
using PBMessage;

public static partial class NetManager
{
    #region 网络事件管理
    
    /// <summary>
    /// 网络事件
    /// </summary>
    public enum NetEvent
    {
        ConnectSuccess = 1,
        ConnectFail = 2,
        Close,
    }

    /// <summary>
    /// 执行的事件
    /// </summary>
    public delegate void EventListener(string err);

    /// <summary>
    /// 事件的字典
    /// </summary>
    private static Dictionary<NetEvent, EventListener> _eventListeners = new Dictionary<NetEvent, EventListener>();

    /// <summary>
    /// 添加事件
    /// </summary>
    /// <param name="netEvent"></param>
    /// <param name="listener"></param>
    public static void AddEventListener(NetEvent netEvent, EventListener listener)
    {
        if (_eventListeners.ContainsKey(netEvent))
        {
            _eventListeners[netEvent] += listener;
        }
        else
        {
            _eventListeners.Add(netEvent, listener);
        }
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    /// <param name="netEvent"></param>
    /// <param name="listener"></param>
    public static void RemoveListener(NetEvent netEvent, EventListener listener)
    {
        if (_eventListeners.ContainsKey(netEvent))
        {
            _eventListeners[netEvent] -= listener;
            if (_eventListeners[netEvent] == null)
            {
                _eventListeners.Remove(netEvent);
            }
        }
    }

    /// <summary>
    /// 分发事件
    /// </summary>
    /// <param name="netEvent"></param>
    /// <param name="err"></param>
    public static void DispatchEvent(NetEvent netEvent, string err)
    {
        if (_eventListeners.ContainsKey(netEvent))
        {
            _eventListeners[netEvent](err);
        }
    }
    
    #endregion

    #region 消息事件管理
    
    /// <summary>
    /// 消息处理委托
    /// </summary>
    public delegate void MsgListener(IExtensible msgBase);

    /// <summary>
    /// 消息事件字典
    /// </summary>
    private static Dictionary<string, MsgListener> _msgListeners = new Dictionary<string, MsgListener>();

    /// <summary>
    /// 添加事件
    /// </summary>
    /// <param name="msgName">事件名字</param>
    /// <param name="listener"></param>
    public static void AddMsgListener(string msgName, MsgListener listener)
    {
        if (_msgListeners.ContainsKey(msgName))
        {
            _msgListeners[msgName] += listener;
        }
        else
        {
            _msgListeners.Add(msgName, listener);
        }
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    /// <param name="msgName"></param>
    /// <param name="listener"></param>
    public static void RemoveMsgListener(string msgName, MsgListener listener)
    {
        if (_msgListeners.ContainsKey(msgName))
        {
            _msgListeners[msgName] -= listener;
            if (_msgListeners[msgName] == null)
            {
                _msgListeners.Remove(msgName);
            }
        }
    }

    /// <summary>
    /// 分发事件
    /// </summary>
    /// <param name="msgName">消息名字</param>
    /// <param name="msgBase">消息体</param>
    public static void DispatchMsg(string msgName, IExtensible msgBase)
    {
        if (_msgListeners.ContainsKey(msgName))
        {
            _msgListeners[msgName](msgBase);
        }
    }
    
    #endregion
}
