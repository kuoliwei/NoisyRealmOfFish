using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HighPolyPageGenerator : MonoBehaviour
{
    [SerializeField] private int xSegments = 60;
    [SerializeField] private int ySegments = 80;

    private void Start()
    {
        GenerateMesh();
    }

    private void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = $"RuntimePageMesh_{xSegments}x{ySegments}";

        int vertCount = (xSegments + 1) * (ySegments + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uv = new Vector2[vertCount];
        int[] triangles = new int[xSegments * ySegments * 6];

        int index = 0;

        for (int y = 0; y <= ySegments; y++)
        {
            for (int x = 0; x <= xSegments; x++)
            {
                float xf = (float)x / xSegments;
                float yf = (float)y / ySegments;

                vertices[index] = new Vector3(xf - 0.5f, yf - 0.5f, 0f);
                uv[index] = new Vector2(xf, yf);
                index++;
            }
        }

        int ti = 0;
        for (int y = 0; y < ySegments; y++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int bl = y * (xSegments + 1) + x;
                int br = bl + 1;
                int tl = bl + (xSegments + 1);
                int tr = tl + 1;

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

        GetComponent<MeshFilter>().sharedMesh = mesh;

        Debug.Log($"High poly mesh generated: {mesh.name}");
    }
}
