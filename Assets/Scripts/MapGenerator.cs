using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

    public enum DrawMode { NoiseMap, ColourMap, Mesh};
    public DrawMode drawMode;
	public const int mapChunkSize = 241;
	[Range(0,6)]
	public int levelOfDetail;
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

    public void generateMap() {
        float[,] noiseMap = Noise.generateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++) {
                    if (currentHeight <= regions[i].height) {
                        colourMap[y * mapChunkSize + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap) {
            display.drawTexture(TextureGenerator.textureFromHeightMap(noiseMap));
        } else if (drawMode == DrawMode.ColourMap) {
            display.drawTexture(TextureGenerator.textureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
		} else if(drawMode == DrawMode.Mesh) {
			display.drawMesh (MeshGenerator.generateTerrainMesh(noiseMap, heightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.textureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
		}
        
    }

    public void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }

        if (octaves < 0) {
            lacunarity = 0;
        }
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color colour; 
}
