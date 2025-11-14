using UnityEngine;
using UnityEditor;

public class CreatePageMeshWindow : EditorWindow
{
    private int xSegments = 40;
    private int ySegments = 60;

    [MenuItem("Tools/Page Mesh Generator")]
    public static void OpenWindow()
    {
        GetWindow<CreatePageMeshWindow>("Page Mesh Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("High Poly Page Mesh Generator", EditorStyles.boldLabel);

        xSegments = EditorGUILayout.IntSlider("X Segments", xSegments, 2, 200);
        ySegments = EditorGUILayout.IntSlider("Y Segments", ySegments, 2, 200);

        if (GUILayout.Button("Generate Page Mesh"))
        {
            CreateMesh(xSegments, ySegments);
        }
    }

    private void CreateMesh(int xSeg, int ySeg)
    {
        Mesh mesh = new Mesh();
        mesh.name = $"PageMesh_{xSeg}x{ySeg}";

        int vertCount = (xSeg + 1) * (ySeg + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uv = new Vector2[vertCount];
        int[] triangles = new int[xSeg * ySeg * 6];

        // ====== 建立頂點 ======
        int index = 0;
        for (int y = 0; y <= ySeg; y++)
        {
            for (int x = 0; x <= xSeg; x++)
            {
                float xf = (float)x / xSeg;
                float yf = (float)y / ySeg;

                // 中心在 (0,0)，Z=0 是平面
                vertices[index] = new Vector3(xf - 0.5f, yf - 0.5f, 0f);
                uv[index] = new Vector2(xf, yf);
                index++;
            }
        }

        // ====== 建立三角形 ======
        int ti = 0;
        for (int y = 0; y < ySeg; y++)
        {
            for (int x = 0; x < xSeg; x++)
            {
                int bl = y * (xSeg + 1) + x;      // bottom-left
                int br = bl + 1;                  // bottom-right
                int tl = bl + (xSeg + 1);         // top-left
                int tr = tl + 1;                  // top-right

                triangles[ti++] = bl;
                triangles[ti++] = tl;
                triangles[ti++] = br;

                triangles[ti++] = br;
                triangles[ti++] = tl;
                triangles[ti++] = tr;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string path = $"Assets/PageMesh_{xSeg}x{ySeg}.asset";
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Created mesh: {path}");
    }
}
