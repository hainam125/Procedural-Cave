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

        int borderSize = 1;
        int[,] borderMap = new int[width + borderSize * 2, height + borderSize * 2];

        for (int x = 0; x < borderMap.GetLength(0); x++) {
            for (int y = 0; y < borderMap.GetLength(1); y++) {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize) {
                    borderMap[x, y] = map[x - borderSize, y - borderSize];
                }
                else {
                    borderMap[x, y] = 1;
                }
            }
        }

        var meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderMap, 1);
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
}
