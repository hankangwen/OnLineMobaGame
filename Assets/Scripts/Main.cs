using PBMessage;
using ProtoBuf;
using UnityEngine;

public class Main : MonoBehaviour
{
    private void Start()
    {
        NetManager.AddEventListener(NetManager.NetEvent.ConnectSuccess, OnEventSuccess);
        NetManager.AddEventListener(NetManager.NetEvent.ConnectFail, OnEventFail);
        NetManager.AddEventListener(NetManager.NetEvent.Close, OnEventClose);
        
        NetManager.AddMsgListener("MsgTest", OnMsgTest);
        NetManager.Connect("127.0.0.1", 8888);
        
        Invoke("Test", 2);
    }

    private void Test()
    {
        MsgTest msgTest = new MsgTest();
        NetManager.Send(msgTest, NetManager.ServerType.Fighter);
    }

    private void OnMsgTest(IExtensible msgBase)
    {
        Debug.Log($"收到消息:{msgBase.ToString()}");
    }

    private void Update()
    {
        NetManager.Update();
    }

    private void OnEventClose(string err)
    {
        Debug.Log("连接关闭");
    }

    private void OnEventFail(string err)
    {
        Debug.Log("连接失败");
    }

    private void OnEventSuccess(string err)
    {
        Debug.Log("连接成功");
    }
}
