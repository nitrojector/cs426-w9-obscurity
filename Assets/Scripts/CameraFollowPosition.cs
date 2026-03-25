using UnityEngine;
using UnityEngine.InputSystem;

public enum CameraMode
{
	Follow,
	FreeLook
}

[DisallowMultipleComponent]
public class CameraFollowPosition : MonoBehaviour
{
	[Header("Target")]
	[Tooltip("If null, the script will automatically find the first GameObject tagged 'Player'.")]
	[SerializeField]
	private Transform target;

	[Header("Camera Mode")] [SerializeField]
	private CameraMode cameraMode = CameraMode.Follow;

	[Tooltip("Key to toggle between Follow and FreeLook (debug only)")] [SerializeField]
	private Key debugToggleModeKey = Key.Tab;

	[Header("Follow Mode (Orthographic)")] [Tooltip("Orthographic camera size")] [SerializeField]
	private float orthographicSize = 10f;

	[Header("FreeLook Mode (Perspective)")] [Tooltip("Field of view for perspective camera")] [SerializeField]
	private float perspectiveFOV = 60f;

	[Tooltip("Mouse sensitivity for rotating around target")] [SerializeField]
	private float mouseSensitivity = 2f;

	[Tooltip("Higher values reduce jitter but keep controls responsive (8-20 is typical)")] [SerializeField]
	private float mouseSmoothing = 12f;

	// Captured offset in Follow mode
	private Vector3 followOffset;

	// FreeLook rotation state (yaw, pitch)
	private float freelookYaw;
	private float freelookPitch;
	private Vector2 smoothedMouseDelta;

	private bool initialized;

	private UnityEngine.Camera cachedCamera;


	private void Awake()
	{
		cachedCamera = GetComponent<UnityEngine.Camera>();
		if (cachedCamera == null)
		{
			Debug.LogError($"{nameof(CameraFollowPosition)}: Camera component not found!");
			enabled = false;
			return;
		}

		ResolveTarget();
	}

	private void Start()
	{
		if (target != null)
		{
			InitializeMode();
		}
	}

	/// <summary>
	/// Programmatically transition to Follow mode (orthographic).
	/// Captures the current relative position from the target.
	/// </summary>
	public void TransitionToFollowMode()
	{
		if (cameraMode == CameraMode.Follow) return;

		cameraMode = CameraMode.Follow;
		InitializeFollowMode();
	}

	/// <summary>
	/// Programmatically transition to FreeLook mode (perspective).
	/// Initializes yaw/pitch from current camera position relative to target.
	/// </summary>
	public void TransitionToFreeLookMode()
	{
		if (cameraMode == CameraMode.FreeLook) return;

		cameraMode = CameraMode.FreeLook;
		InitializeFreeLookMode();
	}

	public void SetFollowTarget(Transform newTarget)
	{
		target = newTarget;
		if (target != null)
		{
			InitializeMode();
		}
	}

	private void ResolveTarget()
	{
		if (target != null) return;

		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
			target = player.transform;
		else
			Debug.LogWarning($"{nameof(CameraFollowPosition)}: No GameObject tagged 'Player' found.");
	}

	private void InitializeMode()
	{
		if (cameraMode == CameraMode.Follow)
		{
			InitializeFollowMode();
		}
		else
		{
			InitializeFreeLookMode();
		}

		initialized = true;
	}

	private void InitializeFollowMode()
	{
		if (target == null) return;

		// Capture the current relative position as the follow offset
		followOffset = transform.position - target.position;

		// Set up orthographic projection
		cachedCamera.orthographic = true;
		cachedCamera.orthographicSize = orthographicSize;

		Debug.Log("Camera switched to Follow mode (Orthographic)");
	}

	private void InitializeFreeLookMode()
	{
		if (target == null) return;

		// Set up perspective projection
		cachedCamera.orthographic = false;
		cachedCamera.fieldOfView = perspectiveFOV;

		// Reset smoothing when entering mode to avoid carry-over jumps
		smoothedMouseDelta = Vector2.zero;

		// Calculate yaw and pitch from current camera position relative to target
		Vector3 relativePos = transform.position - target.position;
		freelookYaw = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;

		float horizontalDist = new Vector3(relativePos.x, 0, relativePos.z).magnitude;
		freelookPitch = Mathf.Atan2(relativePos.y, horizontalDist) * Mathf.Rad2Deg;
		freelookPitch = Mathf.Clamp(freelookPitch, 0f, 90f);

		Debug.Log("Camera switched to FreeLook mode (Perspective)");
	}

	private void Update()
	{
		if (!initialized || target == null) return;

		// Debug keypress to toggle mode
		if (Keyboard.current != null && Keyboard.current[debugToggleModeKey].wasPressedThisFrame)
		{
			if (cameraMode == CameraMode.Follow)
				TransitionToFreeLookMode();
			else
				TransitionToFollowMode();
		}

		// Update camera based on mode
		if (cameraMode == CameraMode.Follow)
		{
			UpdateFollowMode();
		}
		else
		{
			UpdateFreeLookMode();
		}
	}

	private void UpdateFollowMode()
	{
		if (target == null) return;

		// Strict follow: always position at target + captured offset
		Vector3 desiredPosition = target.position + followOffset;
		transform.position = desiredPosition;

		// Look at target
		transform.LookAt(target.position);
	}

	private void UpdateFreeLookMode()
	{
		if (target == null) return;

		// Handle mouse input
		if (Mouse.current != null)
		{
			Vector2 rawMouseDelta = Mouse.current.delta.ReadValue();
			float blend = 1f - Mathf.Exp(-mouseSmoothing * Time.deltaTime);
			smoothedMouseDelta = Vector2.Lerp(smoothedMouseDelta, rawMouseDelta, blend);

			// Mouse delta is already frame-based; avoid additional deltaTime scaling.
			freelookYaw += smoothedMouseDelta.x * mouseSensitivity;
			freelookPitch -= smoothedMouseDelta.y * mouseSensitivity;

			// Clamp pitch to top hemisphere (0 to 90 degrees)
			freelookPitch = Mathf.Clamp(freelookPitch, 0f, 90f);
		}

		// Calculate camera position from yaw and pitch
		Vector3 cameraOffset = GetFreeLookOffset();
		Vector3 desiredPosition = target.position + cameraOffset;
		transform.position = desiredPosition;

		// Look at target
		transform.LookAt(target.position);
	}

	private Vector3 GetFreeLookOffset()
	{
		float yawRad = freelookYaw * Mathf.Deg2Rad;
		float pitchRad = freelookPitch * Mathf.Deg2Rad;

		// Distance is inferred from the initial follow offset magnitude
		float distance = followOffset.magnitude;

		float horizontalDist = distance * Mathf.Cos(pitchRad);
		float verticalDist = distance * Mathf.Sin(pitchRad);

		float x = horizontalDist * Mathf.Sin(yawRad);
		float y = verticalDist;
		float z = horizontalDist * Mathf.Cos(yawRad);

		return new Vector3(x, y, z);
	}
}