using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Klak.Ndi;

public class NdiSourceSelector : MonoBehaviour
{
    [SerializeField] private NdiReceiver receiver;
    [SerializeField] private Dropdown dropdown;

    private string[] sources = System.Array.Empty<string>();

    void Start() => RefreshSources();

    public void RefreshSources()
    {
        // 2.0.2 正確列舉方法：靜態屬性 sourceNames
        sources = NdiFinder.sourceNames?.ToArray() ?? System.Array.Empty<string>();

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();

        if (sources.Length == 0)
        {
            dropdown.options.Add(new Dropdown.OptionData("No NDI sources found"));
            dropdown.interactable = false;
            dropdown.RefreshShownValue();
            return;
        }

        dropdown.options.AddRange(sources.Select(n => new Dropdown.OptionData(n)));
        dropdown.interactable = true;
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(OnSelect);
    }

    private void OnSelect(int index)
    {
        if (index < 0 || index >= sources.Length) return;
        receiver.ndiName = sources[index];

        // 可選：強制重連
        // receiver.enabled = false; receiver.enabled = true;
        Debug.Log($"[NDI] Switched to: {receiver.ndiName}");
    }
}
