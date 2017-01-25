using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

	public LODInfo[] detailLevels;
	public static float maxViewDist;
	public Transform player;
	public static Vector2 playerPosition;
	public Material material;

	private Vector2 playerPositionOld;
	private const float SCALE = 2f;
	private const float PLAYER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE = 25f;
	private const float SQR_PLAYER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE = PLAYER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE * PLAYER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE;
	private static MapGenerator mapGenerator;
	private int chunkSize;
	private int chunkVisibleInViewDist;
	private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary= new Dictionary<Vector2, TerrainChunk>();
	private static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk> ();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator> ();
		maxViewDist = detailLevels [detailLevels.Length - 1].visibleDstThresHold;
		chunkSize = MapGenerator.mapChunkSize - 1;
		chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
		updateVisibleChunks ();
	}

	void Update() {
		playerPosition = new Vector2 (player.position.x, player.position.z) / SCALE;

		if ((playerPosition - playerPositionOld).sqrMagnitude > SQR_PLAYER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE) {
			playerPositionOld = playerPosition;
			updateVisibleChunks ();
		}
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
				} else {
					terrainChunkDictionary.Add(playerChunkCoord, new TerrainChunk(playerChunkCoord, chunkSize, detailLevels, transform, material));
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
		MeshCollider meshCollider;
		LODInfo[] detailLevels;
		LODMesh[] lodMeshes;
		LODMesh collisionLODMesh;
		MapData mapData;
		bool mapDataReceived;
		int prevLODIndex = -1; 

		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform paranet, Material material) {
			this.detailLevels = detailLevels;
			position = coord * size;
			bounds = new Bounds(position,Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x,0,position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
			meshRenderer.material = material;
			meshObject.transform.position = positionV3 * SCALE;
			meshObject.transform.parent = paranet;
			meshObject.transform.localScale = Vector3.one * SCALE;
			setVisible(false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++) {
				lodMeshes[i] = new LODMesh(detailLevels[i].lod, updateTerrianChunk);

				if(detailLevels[i].useForCollider) {
					collisionLODMesh = lodMeshes[i];
				}
			}

			mapGenerator.requestMapData(position, OnMapDataReceived);
		}

		void OnMapDataReceived(MapData mapData) {
			this.mapData = mapData;
			mapDataReceived = true;
			Texture2D texture = TextureGenerator.textureFromColourMap (mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
			meshRenderer.material.mainTexture = texture;
			updateTerrianChunk ();
		}

		public void updateTerrianChunk() {
			if (mapDataReceived) {
				float playerDstFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (playerPosition));
				bool visible = playerDstFromNearestEdge <= maxViewDist;

				if (visible) {
				
					int lodIndex = 0;

					for (int i = 0; i < detailLevels.Length - 1; i++) {
						if (playerDstFromNearestEdge > detailLevels [i].visibleDstThresHold) {
							lodIndex = i + 1;
						} else {
							break;
						}
					}

					if (lodIndex != prevLODIndex) {
						LODMesh lodMesh = lodMeshes [lodIndex];

						if (lodMesh.hasMesh) {
							prevLODIndex = lodIndex;
							meshFilter.mesh = lodMesh.mesh;
						} else if (!lodMesh.hasRequestedMesh) {
							lodMesh.requestMesh (mapData);
						}
					}

					if (lodIndex == 0) {
						if (collisionLODMesh.hasMesh) {
							meshCollider.sharedMesh = collisionLODMesh.mesh;
						} else if (!collisionLODMesh.hasRequestedMesh) {
							collisionLODMesh.requestMesh (mapData);
						}
					}
					terrainChunksVisibleLastUpdate.Add (this);

				}

				setVisible (visible);
			}
		}

		public void setVisible(bool visible) {
			meshObject.SetActive(visible);
		}

		public bool isVisible() {
			return meshObject.activeSelf;
		}

	}

	class LODMesh {

		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;

		private int lod;
		private System.Action updateCallback;

		public LODMesh(int lod, System.Action updateCallback) {
			this.lod = lod;
			this.updateCallback = updateCallback;
		}

		void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.createMesh ();
			hasMesh = true;
			updateCallback ();
		}

		public void requestMesh(MapData mapData) {
			hasRequestedMesh = true;
			mapGenerator.requestMeshData (mapData, lod, OnMeshDataReceived);
		}
	}

	[System.Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDstThresHold;
		public bool useForCollider;
	}
}
