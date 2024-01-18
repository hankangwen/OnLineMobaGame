using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelManager : Singleton<PanelManager>
{
    public enum Layer
    {
        Panel,
        Tip,
    }

    /// <summary>
    /// 层级列表
    /// </summary>
    private Dictionary<Layer, Transform> _layers = new Dictionary<Layer, Transform>();

    /// <summary>
    /// 面板列表
    /// </summary>
    private Dictionary<string, BasePanel> _panels = new Dictionary<string, BasePanel>();
    
    /// <summary>
    /// 面板根级元素
    /// </summary>
    private Transform _root;

    /// <summary>
    /// 画布
    /// </summary>
    private Transform _canvas;

    protected override void Awake()
    {
        base.Awake();
        Init();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private void Init()
    {
        _root = GameObject.Find("Root").transform;
        _canvas = _root.Find("Canvas");
        _layers.Add(Layer.Panel, _canvas.Find("Panel"));
        _layers.Add(Layer.Tip, _canvas.Find("Tip"));
    }

    /// <summary>
    /// 打开面板
    /// </summary>
    /// <param name="args"></param>
    /// <typeparam name="T"></typeparam>
    public void Open<T>(params object[] args) where T : BasePanel
    {
        string name = typeof(T).Name;
        //已经打开
        if (_panels.ContainsKey(name))
        {
            return;
        }

        BasePanel panel = _root.gameObject.AddComponent<T>();
        panel.OnPrepareInit();
        panel.Init();

        Transform layer = _layers[panel.layer];
        panel.skin.transform.SetParent(layer, false);
        _panels.Add(name, panel);
        panel.OnShow();
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    /// <param name="name">面板名字</param>
    public void Close(string name)
    {
        //判断是否有这个面板正在打开
        if (!_panels.ContainsKey(name))
        {
            return;
        }

        BasePanel panel = _panels[name];
        panel.OnClose();
        _panels.Remove(name);
        Destroy(panel.skin);
        Destroy(panel);
    }
}
