using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {

	public static MeshData generateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail) {

		int meshSimplificationIncrement = (levelOfDetail==0)?1:levelOfDetail * 2;
		int borderedSize = heightMap.GetLength (0);
		int meshSize = borderedSize - 2 * meshSimplificationIncrement;
		int meshSizeUnsimplified = borderedSize - 2;

		float topLeftX = (meshSizeUnsimplified -1) / -2f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

		MeshData meshData = new MeshData(verticesPerLine);
		AnimationCurve heightCurve = new AnimationCurve (_heightCurve.keys);

		int[,] vertexIndeciesMap = new int[borderedSize, borderedSize];
		int meshVertexIndex = 0;
		int borderVertexIndex = -1;

		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
				bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

				if (isBorderVertex) {
					vertexIndeciesMap [x, y] = borderVertexIndex;
					borderVertexIndex--;
				} else {
					vertexIndeciesMap [x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			
			}
		}

		for (int y = 0; y < borderedSize; y+=meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x+=meshSimplificationIncrement) {

				int vertexIndex = vertexIndeciesMap [x, y];

				Vector2 percent = new Vector2 ((x-meshSimplificationIncrement)/(float)meshSize, (y-meshSimplificationIncrement)/(float)meshSize);
				float height = heightCurve.Evaluate (heightMap [x, y]) * heightMultiplier;
				Vector3 vertexPosition = new Vector3 (topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

				meshData.addVertex (vertexPosition, percent, vertexIndex);

				if (x < borderedSize - 1 && y < borderedSize - 1) {
					int a = vertexIndeciesMap [x, y];
					int b = vertexIndeciesMap [x + meshSimplificationIncrement, y];
					int c = vertexIndeciesMap [x, y + meshSimplificationIncrement];
					int d = vertexIndeciesMap [x + meshSimplificationIncrement, y + meshSimplificationIncrement];
					meshData.addTriangle(a,d,c);
					meshData.addTriangle(d,a,b);
				}
				vertexIndex ++;
			}
		}

		meshData.bakeNormals ();

		return meshData;

	}
}


public class MeshData {

	private Vector3[] vertices;
	private int[] triangles;
 	private Vector2[] uvs;
	private int triangleindex;
	private Vector3[] borderVertices;
	private Vector3[] bakedNormals;
	private int[] borderTriangles;
	private int triangleIndex;
	private int borderTriangleIndex;


	public MeshData(int verticesPerLine) {
		vertices = new Vector3[verticesPerLine * verticesPerLine];
		uvs = new Vector2[verticesPerLine * verticesPerLine];
		triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

		borderVertices = new Vector3[verticesPerLine * 4 + 4];
		borderTriangles= new int[24 * verticesPerLine];
	}

	public void addVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
		if (vertexIndex < 0) {
			borderVertices [-vertexIndex - 1] = vertexPosition;
		} else {
			vertices [vertexIndex] = vertexPosition;
			uvs [vertexIndex] = uv;
		}
	}

	public void addTriangle(int a, int b, int c) {

		if (a < 0 || b < 0 || c < 0) {
			borderTriangles [borderTriangleIndex] = a;
			borderTriangles [borderTriangleIndex + 1] = b;
			borderTriangles [borderTriangleIndex + 2] = c;
			borderTriangleIndex += 3;
		} else {
			triangles [triangleindex] = a;
			triangles [triangleindex + 1] = b;
			triangles [triangleindex + 2] = c;
			triangleindex += 3;
		}
	}

	private Vector3[] calculateNormals() {

		Vector3[] vertextNormals = new Vector3[vertices.Length];
		int triangleCount = triangles.Length / 3;

		for (int i = 0; i < triangleCount; i++) {
			int normalTrinagleIndex = i * 3;
			int vertextIndexA = triangles [normalTrinagleIndex];
			int vertextIndexB = triangles [normalTrinagleIndex + 1];
			int vertextIndexC = triangles [normalTrinagleIndex + 2];

			Vector3 trinagleNormal = surfaceNormalsFromIndices (vertextIndexA, vertextIndexB, vertextIndexC);
			vertextNormals [vertextIndexA] += trinagleNormal;
			vertextNormals [vertextIndexB] += trinagleNormal;
			vertextNormals [vertextIndexC] += trinagleNormal;
		}

		int borderTriangleCount = borderTriangles.Length / 3;

		for (int i = 0; i < borderTriangleCount; i++) {
			int normalTrinagleIndex = i * 3;
			int vertextIndexA = borderTriangles [normalTrinagleIndex];
			int vertextIndexB = borderTriangles [normalTrinagleIndex + 1];
			int vertextIndexC = borderTriangles [normalTrinagleIndex + 2];

			Vector3 trinagleNormal = surfaceNormalsFromIndices (vertextIndexA, vertextIndexB, vertextIndexC);
			if(vertextIndexA >= 0) {
				vertextNormals [vertextIndexA] += trinagleNormal;
			}
			if(vertextIndexB >= 0) {
				vertextNormals [vertextIndexB] += trinagleNormal;
			}
			if(vertextIndexC >= 0) {
				vertextNormals [vertextIndexC] += trinagleNormal;
			}
		}

		for (int i = 0; i < vertextNormals.Length; i++) {
			vertextNormals [i].Normalize ();
		}

		return vertextNormals;

	}


	private Vector3 surfaceNormalsFromIndices(int indexA, int indexB, int indexC) {
		Vector3 pointA = (indexA < 0) ? borderVertices[-indexA-1] : vertices [indexA];
		Vector3 pointB = (indexB < 0) ? borderVertices[-indexB-1] : vertices [indexB];
		Vector3 pointC = (indexC < 0) ? borderVertices[-indexC-1] : vertices [indexC];

		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;

		return Vector3.Cross (sideAB, sideAC).normalized;
	}

	public void bakeNormals() {
		bakedNormals = calculateNormals ();
	}

	public Mesh createMesh() {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.normals = bakedNormals;
		return mesh;
	}
}