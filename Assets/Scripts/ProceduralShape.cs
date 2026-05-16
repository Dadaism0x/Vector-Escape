using UnityEngine;

public class ProceduralShape : MonoBehaviour
{
    public enum ShapeType { Arrow, Triangle, Square, Pentagon, Hexagon }

    private MeshRenderer meshRenderer;
    private Color currentColor;

    public Color CurrentColor => currentColor;

    public static ShapeType RandomPolygon()
    {
        ShapeType[] options = { ShapeType.Triangle, ShapeType.Square, ShapeType.Pentagon, ShapeType.Hexagon };
        return options[Random.Range(0, options.Length)];
    }

    public void Initialize(ShapeType shape, Color color)
    {
        currentColor = color;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        int sortOrder = sr != null ? sr.sortingOrder : 0;
        string sortLayer = sr != null ? sr.sortingLayerName : "Default";
        if (sr != null) sr.enabled = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");

        // Outline child (rendered behind)
        GameObject ol = new GameObject("_outline");
        ol.transform.SetParent(transform, false);
        ol.transform.localScale = Vector3.one * 1.14f;
        ol.layer = gameObject.layer;
        MeshFilter olf = ol.AddComponent<MeshFilter>();
        MeshRenderer olr = ol.AddComponent<MeshRenderer>();
        olr.material = new Material(shader);
        olr.material.color = new Color(color.r * 0.2f, color.g * 0.2f, color.b * 0.2f, 0.65f);
        olr.sortingLayerName = sortLayer;
        olr.sortingOrder = sortOrder - 1;
        olf.mesh = BuildMesh(shape);

        // Visual child (rendered on top)
        GameObject vis = new GameObject("_visual");
        vis.transform.SetParent(transform, false);
        vis.layer = gameObject.layer;
        MeshFilter mf = vis.AddComponent<MeshFilter>();
        meshRenderer = vis.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(shader);
        meshRenderer.material.color = color;
        meshRenderer.sortingLayerName = sortLayer;
        meshRenderer.sortingOrder = sortOrder;
        mf.mesh = BuildMesh(shape);
    }

    public void SetColor(Color color)
    {
        currentColor = color;
        if (meshRenderer != null)
            meshRenderer.material.color = color;

        Transform ol = transform.Find("_outline");
        if (ol != null)
        {
            MeshRenderer or = ol.GetComponent<MeshRenderer>();
            if (or != null)
                or.material.color = new Color(color.r * 0.2f, color.g * 0.2f, color.b * 0.2f, 0.65f);
        }
    }

    Mesh BuildMesh(ShapeType shape)
    {
        GetShapeData(shape, out Vector2[] verts2D, out int[] tris);

        Mesh mesh = new Mesh();
        Vector3[] vertices = System.Array.ConvertAll(verts2D, v => new Vector3(v.x, v.y, 0f));
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void GetShapeData(ShapeType shape, out Vector2[] verts, out int[] tris)
    {
        if (shape == ShapeType.Arrow)
        {
            verts = new Vector2[]
            {
                new Vector2(  0.00f,  0.50f),  // 0 nose
                new Vector2(  0.42f, -0.06f),  // 1 right wing tip
                new Vector2(  0.18f, -0.06f),  // 2 right wing root
                new Vector2(  0.18f, -0.50f),  // 3 right tail
                new Vector2( -0.18f, -0.50f),  // 4 left tail
                new Vector2( -0.18f, -0.06f),  // 5 left wing root
                new Vector2( -0.42f, -0.06f),  // 6 left wing tip
            };
            tris = new int[]
            {
                0, 1, 2,
                0, 2, 5,
                0, 5, 6,
                2, 3, 4,
                2, 4, 5,
            };
        }
        else
        {
            int sides = shape == ShapeType.Triangle ? 3
                      : shape == ShapeType.Square   ? 4
                      : shape == ShapeType.Pentagon ? 5
                      : 6;

            verts = new Vector2[sides];
            float startAngle = 90f * Mathf.Deg2Rad;
            for (int i = 0; i < sides; i++)
            {
                float angle = startAngle - (2f * Mathf.PI / sides) * i;
                verts[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.5f;
            }

            int triCount = sides - 2;
            tris = new int[triCount * 3];
            for (int i = 0; i < triCount; i++)
            {
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
        }
    }
}
