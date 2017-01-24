using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

    /*
     * Generates a noisemap
     * 
     * @param int width
     * @param int height
     * @param int seed
     * @param float scale
     * @param int octaves
     * @param float persistance
     * @param float lacunarity
     * @param Vector2 offset
     * @return array
     **/
    public static float[,] generateNoiseMap(int mapWidth, int mapHeight, int seed, float sacle, int octaves, float persistance, float lacunarity, Vector2 offset) {

        float[,] noiseMap       = new float[mapWidth, mapHeight];
        float maxNoiseHeight    = float.MinValue;
        float minNoiseHeight    = float.MaxValue;
        System.Random prng      = new System.Random(seed);
        Vector2[] octaveOffset  = new Vector2[octaves];
        float halfWidth         = mapWidth / 2;
        float haltHeight        = mapHeight / 2;

        for (int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;

            octaveOffset[i] = new Vector2(offsetX, offsetY);
        }

        if (sacle <= 0) {
            sacle = 0.0001f;
        }

        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++) {
                    float sampleX = (x-halfWidth) / sacle * frequency + octaveOffset[i].x;
                    float sampleY = (y-haltHeight) / sacle * frequency + octaveOffset[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                } else if (noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x,y]);
            }
        }
                return noiseMap;
    }
}
