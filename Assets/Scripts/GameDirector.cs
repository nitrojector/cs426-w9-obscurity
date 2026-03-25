using System;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
	public const float GridSize = 4f;
	public const float LevelHeight = 3f;

	public static GameDirector Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(this);
	}

	private void Start()
	{
		LockCursor();
	}

	private void LockCursor()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
}