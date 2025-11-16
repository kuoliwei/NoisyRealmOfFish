using System;
using System.Reflection;
using UnityEngine;

public class NDIForceInclude : MonoBehaviour
{
    void Awake()
    {
        // 這行會在 Build 時強迫 Unity 連結 Klak.Ndi.Runtime.External.dll
        try
        {
            Assembly.Load("Klak.Ndi.Runtime.External");
            Debug.Log("NDIForceInclude: Klak.Ndi.Runtime.External.dll loaded successfully.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("NDIForceInclude: Unable to load external NDI runtime. " + e.Message);
        }
    }
}
