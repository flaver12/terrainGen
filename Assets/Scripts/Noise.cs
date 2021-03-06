﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

	public enum NormalizeMode{ Local, Global };

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
	public static float[,] generateNoiseMap(int mapWidth, int mapHeight, int seed, float sacle, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {

        float[,] noiseMap       = new float[mapWidth, mapHeight];
        float maxLocalNoiseHeight    = float.MinValue;
        float minLocalNoiseHeight    = float.MaxValue;
        System.Random prng      = new System.Random(seed);
        Vector2[] octaveOffset  = new Vector2[octaves];
        float halfWidth         = mapWidth / 2;
        float haltHeight        = mapHeight / 2;
		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

        for (int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;

            octaveOffset[i] = new Vector2(offsetX, offsetY);
			maxPossibleHeight += amplitude;
			amplitude *= persistance;
        }

        if (sacle <= 0) {
            sacle = 0.0001f;
        }

        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++) {
					float sampleX = (x-halfWidth + octaveOffset[i].x) / sacle * frequency;
					float sampleY = (y-haltHeight + octaveOffset[i].y) / sacle * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

				if (noiseHeight > maxLocalNoiseHeight) {
                    maxLocalNoiseHeight = noiseHeight;
                } else if (noiseHeight < minLocalNoiseHeight) {
					minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
				if (normalizeMode == NormalizeMode.Local) {
					noiseMap [x, y] = Mathf.InverseLerp (minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap [x, y]);
				} else {
					float normalizedHeight = (noiseMap [x, y] + 1) / (2f * maxPossibleHeight / 2f);
					noiseMap [x, y] = Mathf.Clamp(normalizedHeight,0, int.MaxValue);
				}
            }
        }
        return noiseMap;
    }
}
