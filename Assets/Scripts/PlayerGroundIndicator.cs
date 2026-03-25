using UnityEngine;

public class PlayerGroundIndicator : MonoBehaviour
{
	[SerializeField] private float radius = 0.5f;
	[SerializeField] private Color color = new Color(1f, 1f, 1f, 0.3f);

	private void Awake()
	{
		var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		quad.transform.SetParent(transform);
		quad.transform.localPosition = new Vector3(0f, 0.01f, 0f); // just above ground
		quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		quad.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

		// remove collider, we don't want it interacting physically
		Destroy(quad.GetComponent<Collider>());

		var mat = new Material(Shader.Find("Custom/ToonShader"));
		mat.color = color;
		mat.SetFloat("_Surface", 1); // transparent
		quad.GetComponent<MeshRenderer>().material = mat;
	}
}