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

        ProcessMap();

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
                if (IsInMapRange(x, y)) {
                    if (x != gridX || y != gridY) wallCount += map[x, y];
                }
                else {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    private void ProcessMap() {
        var wallRegions = GetRegions(1);
        var wallThreshold = 50;
        foreach(var wallRegion in wallRegions) {
            if(wallRegion.Count < wallThreshold) {
                foreach(var tile in wallRegion) {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }

        var roomRegions = GetRegions(0);
        var roomThreshold = 50;
        var survivingRooms = new List<Room>();
        foreach (var roomRegion in roomRegions) {
            if (roomRegion.Count < roomThreshold) {
                foreach (var tile in roomRegion) {
                    map[tile.tileX, tile.tileY] = 1;
                }
            }
            else {
                survivingRooms.Add(new Room(roomRegion, map));
            }
        }

        survivingRooms.Sort();
        survivingRooms[0].isMainRoom = true;
        survivingRooms[0].isAccessibleFromMainRoom = true;
        CoonectClosestRooms(survivingRooms);
    }

    private void CoonectClosestRooms(List<Room> allRooms, bool forceAccessibilityMainRoom = false) {
        var roomListA = new List<Room>();
        var roomListB = new List<Room>();

        if (forceAccessibilityMainRoom) {
            foreach(var room in allRooms) {
                if (room.isAccessibleFromMainRoom) {
                    roomListB.Add(room);
                }
                else {
                    roomListA.Add(room);
                }
            }
        }
        else {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        var bestDistance = 0;
        var bestTileA = new Coord();
        var bestTileB = new Coord();
        var bestRoomA = new Room();
        var bestRoomB = new Room();
        var possibleConnectionFound = false;

        foreach (var roomA in roomListA) {
            if (!forceAccessibilityMainRoom) {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0) {
                    continue;
                }
            }
            foreach (var roomB in roomListB) {
                if (roomA == roomB || roomA.IsConnected(roomB)) continue;
                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++) {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++) {
                        var tileA = roomA.edgeTiles[tileIndexA];
                        var tileB = roomB.edgeTiles[tileIndexB];
                        var distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound) {
                            possibleConnectionFound = true;
                            bestDistance = distanceBetweenRooms;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }
            if (possibleConnectionFound && !forceAccessibilityMainRoom) {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }


        if (possibleConnectionFound && forceAccessibilityMainRoom) {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            CoonectClosestRooms(allRooms, true);
        }

        if (!forceAccessibilityMainRoom) {
            CoonectClosestRooms(allRooms, true);
        }
    }

    private void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB) {
        Room.ConnectRoom(roomA, roomB);
        Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.white, 100);
    }

    private Vector3 CoordToWorldPoint(Coord tile) {
        return new Vector3(-width / 2 + 0.5f + tile.tileX, 0, -height / 2 + 0.5f + tile.tileY);
    }

    private List<List<Coord>> GetRegions(int tileType) {
        var regions = new List<List<Coord>>();
        var mapFlags = new int[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if(mapFlags[x,y]==0 && map[x, y] == tileType) {
                    var newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);
                    foreach (var tile in newRegion) mapFlags[tile.tileX, tile.tileY] = 1;
                }

            }
        }

        return regions;
    }

    private List<Coord> GetRegionTiles(int startX, int startY) {
        var tiles = new List<Coord>();
        var mapFlags = new int[width, height];
        var tileType = map[startX, startY];

        var queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0) {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                    if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX)) {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType) {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    private bool IsInMapRange(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private struct Coord {
        public int tileX, tileY;

        public Coord(int x, int y) {
            tileX = x;
            tileY = y;
        }
    }

    private class Room : System.IComparable<Room> {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public Room() {
        }

        public Room(List<Coord> roomTiles, int[,] map) {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach (var tile in tiles) {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
                    if (CheckEdge(tile, map)) edgeTiles.Add(tile);
                }
            }
        }


        private bool CheckEdge(Coord tile, int[,] map) {
            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                    if (x == tile.tileX || y == tile.tileY) {
                        if (map[x, y] == 1) return true;
                    }
                }
            }
            return false;
        }

        public bool IsConnected(Room other) {
            return connectedRooms.Contains(other);
        }

        public int CompareTo(Room other) {
            return other.roomSize.CompareTo(roomSize);
        }

        public void SetAccessableFromMainRoom() {
            if (!isAccessibleFromMainRoom) {
                isAccessibleFromMainRoom = true;
                foreach (var room in connectedRooms) room.SetAccessableFromMainRoom();
            }
        }

        public static void ConnectRoom(Room roomA, Room roomB) {
            if (roomA.isAccessibleFromMainRoom) {
                roomB.SetAccessableFromMainRoom();
            }
            else if (roomB.isAccessibleFromMainRoom) {
                roomA.SetAccessableFromMainRoom();
            }
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }
    }
}
