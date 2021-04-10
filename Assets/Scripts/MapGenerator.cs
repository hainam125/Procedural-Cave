using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {
    [SerializeField] private int width, height;
    [SerializeField] [Range(0,100)] private int randomFillPercent;
    [SerializeField] private string seed;
    [SerializeField] private bool useRandomSeed;

    private int[,] map;


    private void Start() {
        GenerateMap();
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            GenerateMap();
        }
    }

    private void GenerateMap() {
        map = new int[width,height];

        RandomFillMap();

        for (int i = 0; i < 5; i++) {
            SmoothMap();
        }

        var meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(map, 1);
    }

    private void RandomFillMap() {
        if (useRandomSeed) {
            seed = Random.Range(0f, 1f).ToString();
        }
        var pseudoRandom = new System.Random(seed.GetHashCode());
        for(int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) {
                    map[x, y] = 1;
                }
                else {
                    map[x, y] = pseudoRandom.Next(0, 100) < randomFillPercent ? 1 : 0;
                }
            }
        }
    }

    private void SmoothMap() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int neighbourWallTile = GetSurroundWallCount(x, y);
                if (neighbourWallTile > 4) {
                    map[x, y] = 1;
                }
                else if (neighbourWallTile < 4) {
                    map[x, y] = 0;
                }
            }
        }
    }

    private int GetSurroundWallCount(int gridX, int gridY) {
        int wallCount = 0;
        for (int x = gridX - 1; x <= gridX + 1; x++) {
            for (int y = gridY - 1; y <= gridY + 1; y++) {
                if (x >= 0 && x < width && y >= 0 && y < height) {
                    if (x != gridX || y != gridY) wallCount += map[x, y];
                }
                else {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    /*private void OnDrawGizmos() {
        if (map != null) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    Gizmos.color = (map[x, y] == 1) ? Color.black : Color.white;
                    var pos = new Vector3(-width / 2 + x + 0.5f, 0, -height / 2 + y + 0.5f);
                    Gizmos.DrawCube(pos, Vector3.one);
                }
            }
        }
    }*/
}
