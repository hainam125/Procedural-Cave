using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour {
    public SquareGrid squareGrid;

    public void GenerateMesh(int[,] map, float squareSize) {
        squareGrid = new SquareGrid(map, squareSize);
    }

    private void OnDrawGizmos() {
        if(squareGrid != null) {
            var squares = squareGrid.squares;
            for (int x = 0; x < squares.GetLength(0); x++) {
                for (int y = 0; y < squares.GetLength(1); y++) {
                    Gizmos.color = squares[x, y].topLeft.active ? Color.black : Color.white;
                    Gizmos.DrawCube(squares[x, y].topLeft.pos, Vector3.one * 0.4f);
                    Gizmos.color = squares[x, y].topRight.active ? Color.black : Color.white;
                    Gizmos.DrawCube(squares[x, y].topRight.pos, Vector3.one * 0.4f);
                    Gizmos.color = squares[x, y].botoomRight.active ? Color.black : Color.white;
                    Gizmos.DrawCube(squares[x, y].botoomRight.pos, Vector3.one * 0.4f);
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
        public ControlNode topLeft, topRight, botoomRight, bottomLeft;
        public Node centerTop, centerRight, centerBottom, centerLeft;

        public Square(ControlNode topLeft, ControlNode topRight, ControlNode botoomRight, ControlNode bottomLeft) {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.botoomRight = botoomRight;
            this.bottomLeft = bottomLeft;

            centerTop = topLeft.right;
            centerRight = botoomRight.above;
            centerBottom = bottomLeft.right;
            centerLeft = bottomLeft.above;
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
