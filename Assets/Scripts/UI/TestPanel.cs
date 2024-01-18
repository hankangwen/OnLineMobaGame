using System.Collections;
using System.Collections.Generic;
using PBMessage;
using ProtoBuf;
using UnityEngine;

public class TestPanel : BasePanel
{
    public override void OnPrepareInit()
    {
        base.OnPrepareInit();
        skinPath = "UI/TestPanel";
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        
        NetManager.AddMsgListener("MsgTest", OnMsgTest);
        
        MsgTest msgTest = new MsgTest();
        NetManager.SendTo(msgTest, NetManager.ServerType.Fighter);
    }
    
    public override void OnClose()
    {
        base.OnClose();
    }
    
    private void OnMsgTest(IExtensible msgBase)
    {
        Debug.Log($"收到消息:{msgBase.ToString()}");
    }
}
