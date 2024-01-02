using System;
using UnityEngine;

public class Main : MonoBehaviour
{
    private void Start()
    {
        NetManager.Connect("127.0.0.1", 8888);
    }
}
