using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

	public const float maxViewDist = 300;
	public Transform player;
	public static Vector2 playerPosition;

	private int chunkSize;
	private int chunkVisibleInViewDist;
	private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary= new Dictionary<Vector2, TerrainChunk>();

	void Start() {
		chunkSize = MapGenerator.mapChunkSize - 1;
		chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);

	}

	private void updateVisibleChunks() {
		int currentChunkCoordX = Mathf.RoundToInt (playerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (playerPosition.y / chunkSize);

		for (int yOffset = -chunkVisibleInViewDist; yOffset <= chunkVisibleInViewDist; yOffset++) {
			for (int xOffset = -chunkVisibleInViewDist; xOffset <= chunkVisibleInViewDist; xOffset++) {
				Vector2 playerChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey (playerChunkCoord)) {
				
				} else {
					terrainChunkDictionary.Add(playerChunkCoord, new TerrainChunk());
				}
			}
		}
	}

	public class TerrainChunk {
		
	}
}
