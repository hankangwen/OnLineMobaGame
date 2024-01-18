using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePanel : MonoBehaviour
{
    /// <summary>
    /// 面板加载路径
    /// </summary>
    public string skinPath;

    /// <summary>
    /// 面板物体
    /// </summary>
    public GameObject skin;

    /// <summary>
    /// 面板层级
    /// </summary>
    public PanelManager.Layer layer = PanelManager.Layer.Panel;
    
    
}
