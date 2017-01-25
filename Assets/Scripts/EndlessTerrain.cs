using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

	public const float maxViewDist = 450;
	public Transform player;
	public static Vector2 playerPosition;
	public Material material;

	private static MapGenerator mapGenerator;
	private int chunkSize;
	private int chunkVisibleInViewDist;
	private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary= new Dictionary<Vector2, TerrainChunk>();
	private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk> ();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator> ();
		chunkSize = MapGenerator.mapChunkSize - 1;
		chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);

	}

	void Update() {
		playerPosition = new Vector2 (player.position.x, player.position.z);
		updateVisibleChunks ();
	}

	private void updateVisibleChunks() {

		for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
			terrainChunksVisibleLastUpdate [i].setVisible (false);
		}
		terrainChunksVisibleLastUpdate.Clear ();

		int currentChunkCoordX = Mathf.RoundToInt (playerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (playerPosition.y / chunkSize);

		for (int yOffset = -chunkVisibleInViewDist; yOffset <= chunkVisibleInViewDist; yOffset++) {
			for (int xOffset = -chunkVisibleInViewDist; xOffset <= chunkVisibleInViewDist; xOffset++) {
				Vector2 playerChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey (playerChunkCoord)) {
					terrainChunkDictionary [playerChunkCoord].updateTerrianChunk ();
					if (terrainChunkDictionary [playerChunkCoord].isVisible ()) {
						terrainChunksVisibleLastUpdate.Add (terrainChunkDictionary [playerChunkCoord]);
					}
				} else {
					terrainChunkDictionary.Add(playerChunkCoord, new TerrainChunk(playerChunkCoord, chunkSize, transform, material));
				}
			}
		}
	}

	public class TerrainChunk {

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;
		MeshRenderer meshRenderer;
		MeshFilter meshFilter;

		public TerrainChunk(Vector2 coord, int size, Transform paranet, Material material) {
			position = coord * size;
			bounds = new Bounds(position,Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x,0,position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;
			meshObject.transform.position = positionV3;
			meshObject.transform.parent = paranet;
			setVisible(false);

			mapGenerator.requestMapData(OnMapDataReceived);
		}

		void OnMapDataReceived(MapData mapData) {
			mapGenerator.requestMeshData (mapData, OnMeshDataReceived);
		}

		void OnMeshDataReceived(MeshData meshData) {
			meshFilter.mesh = meshData.createMesh ();
		}

		public void updateTerrianChunk() {
			float playerDstFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance(playerPosition));
			bool visible = playerDstFromNearestEdge <= maxViewDist;
			setVisible(visible);
		}

		public void setVisible(bool visible) {
			meshObject.SetActive(visible);
		}

		public bool isVisible() {
			return meshObject.activeSelf;
		}

	}

	class LODMesh {
		
	}
}
