using UnityEngine;

/// <summary>
/// Sets up the game field by creating colliders based on the Map data.
/// Each collider is placed at the center of a grid square (0/*/0 is center).
/// Colliders have dimensions of GridSize x (2*LevelHeight) x GridSize.
/// </summary>
public class MapSetup : MonoBehaviour
{
	[SerializeField] private bool generateColliders = true;
	[SerializeField] private Material colliderMaterial;

	private void Awake()
	{
		if (generateColliders)
		{
			GenerateMapColliders();
		}
	}

	/// <summary>
	/// Generates colliders for all solid blocks (value 1) in the map.
	/// </summary>
	private void GenerateMapColliders()
	{
		byte[,] mapData = Map.PrimaryLevelMapData;
		int rows = mapData.GetLength(0);
		int cols = mapData.GetLength(1);

		for (int y = 0; y < rows; y++)
		{
			for (int x = 0; x < cols; x++)
			{
				// Only create colliders for solid blocks (value 1)
				if (mapData[y, x] == 1)
				{
					CreateBlockCollider(x, y);
				}
			}
		}
	}

	/// <summary>
	/// Creates a single block collider at the specified grid coordinates.
	/// The position is at the center of the grid square.
	/// Map is centered around 0/0.
	/// </summary>
	private void CreateBlockCollider(int gridX, int gridY)
	{
		// Create a new GameObject for the collider
		GameObject blockObject = new GameObject($"Block_{gridX}_{gridY}");
		blockObject.transform.parent = transform;

		// Calculate world position (0/*/0 is center of grid square)
		// Map is 13x13, so center is at (6, 6). Offset to center around 0/0.
		Vector3 worldPosition = new Vector3(
			(gridX - 6f) * GameDirector.GridSize,
			0f,
			(gridY - 6f) * GameDirector.GridSize
		);
		blockObject.transform.position = worldPosition;

		// Add box collider
		BoxCollider collider = blockObject.AddComponent<BoxCollider>();
		collider.size = new Vector3(
			GameDirector.GridSize,
			2f * GameDirector.LevelHeight,
			GameDirector.GridSize
		);

		collider.material = new PhysicsMaterial()
		{
			bounciness = 0f,
			dynamicFriction = 0.0f,
			staticFriction = 0.0f,
			frictionCombine = PhysicsMaterialCombine.Minimum,
		};

		// Optionally add a visual mesh (useful for debugging)
		AddVisualMesh(blockObject);
	}

	/// <summary>
	/// Adds a visual mesh to the block for debugging purposes.
	/// Can be disabled by removing this call or by disabling the MeshRenderer.
	/// </summary>
	private void AddVisualMesh(GameObject blockObject)
	{
		// Add mesh filter
		MeshFilter meshFilter = blockObject.AddComponent<MeshFilter>();
		meshFilter.mesh = CreateCubeMesh();

		// Add mesh renderer
		MeshRenderer meshRenderer = blockObject.AddComponent<MeshRenderer>();
		if (colliderMaterial != null)
		{
			meshRenderer.material = colliderMaterial;
		}
		else
		{
			meshRenderer.material = new Material(Shader.Find("Custom/ToonShader"));
		}
	}

	/// <summary>
	/// Creates a simple cube mesh for visual debugging.
	/// </summary>
	private Mesh CreateCubeMesh()
	{
		Mesh mesh = new Mesh();
		mesh.name = "BlockMesh";

		float quaterSize = GameDirector.GridSize / 4f;

		// Define vertices for a cube
		Vector3[] vertices = new Vector3[]
		{
			// Bottom face
			new Vector3(-quaterSize, -quaterSize, -quaterSize), // 0
			new Vector3(quaterSize, -quaterSize, -quaterSize), // 1
			new Vector3(quaterSize, -quaterSize, quaterSize), // 2
			new Vector3(-quaterSize, -quaterSize, quaterSize), // 3
			// Top face
			new Vector3(-quaterSize, quaterSize, -quaterSize), // 4
			new Vector3(quaterSize, quaterSize, -quaterSize), // 5
			new Vector3(quaterSize, quaterSize, quaterSize), // 6
			new Vector3(-quaterSize, quaterSize, quaterSize), // 7
		};

		// Define triangles
		int[] triangles = new int[]
		{
			// Bottom
			0, 2, 1,
			0, 3, 2,
			// Top
			4, 5, 6,
			4, 6, 7,
			// Front
			0, 1, 5,
			0, 5, 4,
			// Back
			2, 3, 7,
			2, 7, 6,
			// Left
			0, 4, 7,
			0, 7, 3,
			// Right
			1, 2, 6,
			1, 6, 5,
		};

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();

		return mesh;
	}
}