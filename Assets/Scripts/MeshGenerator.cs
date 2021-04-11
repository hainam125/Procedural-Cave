using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour {
    [SerializeField] private MeshFilter walls;
    [SerializeField] private MeshFilter cave;
    [SerializeField] private bool is2D;

    private SquareGrid squareGrid;

    private List<Vector3> vertices;
    private List<int> triangles;

    private Dictionary<int, List<Triangle>> triangleDict = new Dictionary<int, List<Triangle>>();
    private List<List<int>> outlines = new List<List<int>>();
    private HashSet<int> checkedVertices = new HashSet<int>();

    public void GenerateMesh(int[,] map, float squareSize) {
        triangleDict.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        squareGrid = new SquareGrid(map, squareSize);
        vertices = new List<Vector3>();
        triangles = new List<int>();

        var squares = squareGrid.squares;
        for (int x = 0; x < squares.GetLength(0); x++) {
            for (int y = 0; y < squares.GetLength(1); y++) {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        cave.mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        int tileAmount = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++) {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].x) * tileAmount;
            float percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].z) * tileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
        mesh.uv = uvs;

        if (is2D) Generate2DColliders();
        else CreateWallMesh();
    }

    private void CreateWallMesh() {
        CalculateOutlines();
        var wallVertices = new List<Vector3>();
        var wallTriangles = new List<int>();
        var colors = new List<Color>();
        var wallMesh = new Mesh();
        var wallHeight = 5f;

        foreach (var outline in outlines) {
            for (int i = 0; i < outline.Count - 1; i++) {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]);//left
                wallVertices.Add(vertices[outline[i + 1]]);//right
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);//bottom left
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight);//bottom right

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);

                colors.Add(Color.white * i / outline.Count);
                colors.Add(Color.white * i / outline.Count);
                colors.Add(Color.white * i / outline.Count);
                colors.Add(Color.white * i / outline.Count);
            }
        }
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.colors = colors.ToArray();
        wallMesh.RecalculateNormals();

        walls.mesh = wallMesh;

        MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;
    }

    private void Generate2DColliders() {
        var currentColliders = gameObject.GetComponents<EdgeCollider2D>();
        foreach (var c in currentColliders) Destroy(c);
        CalculateOutlines();
        foreach(var outline in outlines) {
            var edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            var edgePoints = new Vector2[outline.Count];
            for (int i = 0; i < outline.Count; i++) edgePoints[i] = new Vector2(vertices[outline[i]].x, vertices[outline[i]].z);
            edgeCollider.points = edgePoints;
        }
    }

    private void TriangulateSquare(Square square) {
        //start pos is important;
        switch (square.configuration) {
            case 0:
                break;

            // 1 points:
            case 1:
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;

            // 2 points:
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5:
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 3 point:
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 4 point:
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                break;
        }
        checkedVertices.Add(square.topLeft.vertexIndex);
        checkedVertices.Add(square.topRight.vertexIndex);
        checkedVertices.Add(square.bottomRight.vertexIndex);
        checkedVertices.Add(square.bottomLeft.vertexIndex);
    }

    private void MeshFromPoints(params Node[] points) {
        AssignVertices(points);

        if (points.Length >= 3) CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4) CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5) CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6) CreateTriangle(points[0], points[4], points[5]);

    }

    private void AssignVertices(Node[] points) {
        for(int i = 0; i < points.Length; i++) {
            if(points[i].vertexIndex == -1) {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].pos);
            }
        }
    }

    private void CreateTriangle(Node a, Node b, Node c) {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(a.vertexIndex, triangle);
        AddTriangleToDictionary(b.vertexIndex, triangle);
        AddTriangleToDictionary(c.vertexIndex, triangle);
    }

    private void AddTriangleToDictionary(int vertexIndex, Triangle triangle) {
        if (triangleDict.ContainsKey(vertexIndex)) {
            triangleDict[vertexIndex].Add(triangle);
        }
        else {
            triangleDict.Add(vertexIndex, new List<Triangle>() { triangle });
        }
    }

    private void CalculateOutlines() {
        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++) {
            if (!checkedVertices.Contains(vertexIndex)) {
                int newOutlineVertex = GetConnectOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1) {
                    checkedVertices.Add(vertexIndex);
                    var newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
        SimplifyMeshOutlines();
    }

    private void SimplifyMeshOutlines() {
        for (int outlineIndex = 0; outlineIndex < outlines.Count; outlineIndex++) {
            List<int> simplifiedOutline = new List<int>();
            Vector3 dirOld = Vector3.zero;
            for (int i = 0; i < outlines[outlineIndex].Count; i++) {
                Vector3 p1 = vertices[outlines[outlineIndex][i]];
                Vector3 p2 = vertices[outlines[outlineIndex][(i + 1) % outlines[outlineIndex].Count]];
                Vector3 dir = p1 - p2;
                if (dir != dirOld) {
                    dirOld = dir;
                    simplifiedOutline.Add(outlines[outlineIndex][i]);
                }
            }
            outlines[outlineIndex] = simplifiedOutline;
        }
    }

    private void FollowOutline(int vertexIndex, int outlineIndex) {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectOutlineVertex(vertexIndex);
        if (nextVertexIndex != -1) {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    private int GetConnectOutlineVertex(int vertexIndex) {
        var triangleWithVertex = triangleDict[vertexIndex];
        for (int i = 0; i < triangleWithVertex.Count; i++) {
            var triangle = triangleWithVertex[i];
            for (int j = 0; j < 3; j++) {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB) && IsOutlienEdge(vertexIndex, vertexB)) {
                    return vertexB;
                }
            }
        }
        return -1;
    }

    private bool IsOutlienEdge(int vertexA, int vertexB) {
        var triaglesWithA = triangleDict[vertexA];
        int sharedCount = 0;
        for(int i = 0; i < triaglesWithA.Count; i++) {
            if (triaglesWithA[i].Contains(vertexB)) {
                sharedCount++;
                if (sharedCount > 1) {
                    break;
                }
            }
        }
        return sharedCount == 1;
    }


    public class SquareGrid {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize) {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            var controlNodes = new ControlNode[nodeCountX, nodeCountY];
            for(int x = 0; x < nodeCountX; x++) {
                for (int y = 0; y < nodeCountY; y++) {
                    var pos = new Vector3(-mapWidth / 2 + (x + 0.5f) * squareSize, 0, -mapHeight / 2 + (y + 0.5f) * squareSize);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }
            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++) {
                for (int y = 0; y < nodeCountY - 1; y++) {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }
    }

    public class Square {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centerTop, centerRight, centerBottom, centerLeft;
        //topLeft(2^3)-topRight(2^2)-bottomRight(2^1)-bottomLeft(2^0)
        public int configuration;

        public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft) {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
            this.bottomLeft = bottomLeft;

            centerTop = topLeft.right;
            centerRight = bottomRight.above;
            centerBottom = bottomLeft.right;
            centerLeft = bottomLeft.above;

            if (topLeft.active) configuration += 8;
            if (topRight.active) configuration += 4;
            if (bottomRight.active) configuration += 2;
            if (bottomLeft.active) configuration += 1;
        }
    }

    public class Node {
        public Vector3 pos;
        public int vertexIndex = -1;

        public Node(Vector3 position) {
            pos = position;
        }
    }

    public class ControlNode : Node {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 pos, bool activated, float squareSize) : base(pos) {
            active = activated;
            above = new Node(pos + Vector3.forward * squareSize / 2);
            right = new Node(pos + Vector3.right * squareSize / 2);
        }
    }

    private struct Triangle {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;

        private int[] vertices;

        public Triangle(int a, int b, int c) {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[] { vertexIndexA, vertexIndexB, vertexIndexC };
        }

        public int this[int i] {
            get => vertices[i];
        }

        public bool Contains(int vertexIndex) {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }
}
