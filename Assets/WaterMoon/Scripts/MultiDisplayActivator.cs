using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiDisplayActivator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if !UNITY_EDITOR
        // Display 0 是主顯示器，其他需要手動啟用
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
            Debug.Log($"已啟用 Display {i}");
        }
        Debug.Log($"多顯示器啟動完成，共偵測到 {Display.displays.Length} 個顯示器。");
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
