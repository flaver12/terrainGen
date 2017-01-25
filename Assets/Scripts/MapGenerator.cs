using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {

    public enum DrawMode { NoiseMap, ColourMap, Mesh};
    public DrawMode drawMode;
	public Noise.NormalizeMode normalizeMode;
	public const int mapChunkSize = 241;
	[Range(0,6)]
	public int editorPreviewLOD;
    public int seed;
    public float noiseScale;
    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public Vector2 offset;
	public float heightMultiplier;
	public AnimationCurve meshHeightCurve;
    public TerrainType[] regions;
    public bool autoUpdate;

	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	public void drawMapInEditor() {
		MapData mapData = generateMap (Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay>();
		if (drawMode == DrawMode.NoiseMap) {
			display.drawTexture(TextureGenerator.textureFromHeightMap(mapData.heightMap));
		} else if (drawMode == DrawMode.ColourMap) {
			display.drawTexture(TextureGenerator.textureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
		} else if(drawMode == DrawMode.Mesh) {
			display.drawMesh (MeshGenerator.generateTerrainMesh(mapData.heightMap, heightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.textureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
		}
	}

	public void requestMapData(Vector2 centre, Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			mapDataThread (centre, callback);
		};

		new Thread (threadStart).Start ();
	}

	public void requestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			meshDataThread(mapData, lod, callback);
		};
		new Thread(threadStart).Start();
	}

	private void meshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.generateTerrainMesh (mapData.heightMap, heightMultiplier, meshHeightCurve, lod);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	private void mapDataThread(Vector2 centre, Action<MapData> callback) {
		MapData mapData = generateMap (centre);
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
		}
	}

	void Update() {
		if(mapDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	}

	private MapData generateMap(Vector2 centre) {
		float[,] noiseMap = Noise.generateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizeMode);
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++) {
					if (currentHeight >= regions [i].height) {
						colourMap [y * mapChunkSize + x] = regions [i].colour;
					} else {
						break;
					}
                }
            }
        }
        
		return new MapData (noiseMap, colourMap);
    }

    public void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }

        if (octaves < 0) {
            lacunarity = 0;
        }
    }

	struct MapThreadInfo<T> {
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo(Action<T> callback, T parameter) {
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color colour; 
}

public struct MapData {
	public readonly float[,] heightMap;
	public readonly Color[] colourMap;

	public MapData(float [,] heightMap, Color[] colourMap) {
		this.heightMap = heightMap;
		this.colourMap = colourMap;
	}
}