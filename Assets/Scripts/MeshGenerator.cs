using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour {
    public SquareGrid squareGrid;
    private List<Vector3> vertices;
    private List<int> triangles;

    public void GenerateMesh(int[,] map, float squareSize) {
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
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    private void TriangulateSquare(Square square) {
        switch (square.configuration) {
            case 0:
                break;
            // 1 points:
            case 1:
                MeshFromPoints(square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 2:
                MeshFromPoints(square.centerRight, square.bottomRight, square.centerBottom);
                break;
            case 4:
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight);
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
    }

    /*private void OnDrawGizmos() {
        if(squareGrid != null) {
            var squares = squareGrid.squares;
            for (int x = 0; x < squares.GetLength(0); x++) {
                for (int y = 0; y < squares.GetLength(1); y++) {
                    Gizmos.color = squares[x, y].topLeft.active ? Color.black : Color.white;
                    Gizmos.DrawCube(squares[x, y].topLeft.pos, Vector3.one * 0.4f);
                    Gizmos.color = squares[x, y].topRight.active ? Color.black : Color.white;
                    Gizmos.DrawCube(squares[x, y].topRight.pos, Vector3.one * 0.4f);
                    Gizmos.color = squares[x, y].bottomRight.active ? Color.black : Color.white;
                    Gizmos.DrawCube(squares[x, y].bottomRight.pos, Vector3.one * 0.4f);
                    Gizmos.color = squares[x, y].bottomLeft.active ? Color.black : Color.white;
                    Gizmos.DrawCube(squares[x, y].bottomLeft.pos, Vector3.one * 0.4f);

                    Gizmos.color = Color.gray;
                    Gizmos.DrawCube(squares[x, y].centerTop.pos, Vector3.one * 0.15f);
                    Gizmos.DrawCube(squares[x, y].centerRight.pos, Vector3.one * 0.15f);
                    Gizmos.DrawCube(squares[x, y].centerBottom.pos, Vector3.one * 0.15f);
                    Gizmos.DrawCube(squares[x, y].centerLeft.pos, Vector3.one * 0.15f);
                }
            }
        }
    }*/


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
}
