using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Klak.Ndi;

[System.Serializable]
public class NdiConfigData
{
    public string ndiName = "";
}

public class NdiSourceSelector : MonoBehaviour
{
    [Header("NDI Receiver")]
    [SerializeField] private NdiReceiver receiver;

    [Header("Input Field")]
    [SerializeField] private InputField inputField;

    [Header("JSON Settings")]
    [SerializeField] private string jsonPath = "JsonData";   // 資料夾名稱
    [SerializeField] private string jsonName = "ndi_config"; // 檔名（不含副檔名）

    private string folderPath;
    private string filePath;

    private NdiConfigData config = new NdiConfigData();

    void Awake()
    {
        // 組路徑： <Project>/Assets/JsonData/ndi_config.json
        folderPath = Path.Combine(Application.dataPath, jsonPath);
        filePath = Path.Combine(folderPath, jsonName + ".json");

        SetupFolder();
        SetupJsonFile();
    }

    void Start()
    {
        LoadJson();

        // 若 JSON 有文字，套用 InputField
        if (!string.IsNullOrEmpty(config.ndiName))
        {
            inputField.text = config.ndiName;
            ApplySourceName(config.ndiName);
        }

        // 使用者輸入新值 → 更新 Receiver & JSON
        inputField.onEndEdit.AddListener(OnInputEndEdit);
    }

    // -----------------------------
    // 初期設定
    // -----------------------------
    private void SetupFolder()
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"[NDI] Created folder: {folderPath}");
        }
    }

    private void SetupJsonFile()
    {
        if (!File.Exists(filePath))
        {
            SaveJson(); // 建立新的 JSON（空白欄位）
            Debug.Log($"[NDI] Created json file: {filePath}");
        }
    }

    // -----------------------------
    // 載入 / 儲存 JSON
    // -----------------------------
    private void LoadJson()
    {
        try
        {
            string json = File.ReadAllText(filePath);
            if (!string.IsNullOrEmpty(json))
            {
                config = JsonUtility.FromJson<NdiConfigData>(json);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[NDI] Failed to load json: " + e.Message);
        }
    }

    private void SaveJson()
    {
        try
        {
            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(filePath, json);
            Debug.Log("[NDI] Saved json: " + config.ndiName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[NDI] Failed to save json: " + e.Message);
        }
    }

    // -----------------------------
    // UI → NDI Receiver 更新
    // -----------------------------
    private void OnInputEndEdit(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        config.ndiName = value;
        SaveJson();

        ApplySourceName(value);
    }

    private void ApplySourceName(string ndiName)
    {
        receiver.ndiName = ndiName;

        Debug.Log($"[NDI] Receiver now uses: {ndiName}");
    }
}
