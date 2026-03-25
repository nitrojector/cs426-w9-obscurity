using System.Collections;
using UnityEngine;

/// <summary>
/// 2D maze on the XY plane: invisible wall colliders + sprites (disabled until goal),
/// orthographic camera, Rigidbody2D ball.
/// </summary>
public class InvisibleMazeGame : MonoBehaviour
{
    [Header("Difficulty")]
    [Tooltip("Odd sizes work best; formula clamps to a sane maximum.")]
    public int baseGridSize = 9;
    public int gridSizePerLevel = 2;
    public int maxGridSize = 35;
    [Tooltip("How many mazes to complete before the game ends (plays the big victory sound on the last).")]
    [Min(1)]
    public int totalLevels = 10;

    [Header("Prefabs / visuals")]
    public Color wallRevealedColor = new Color(0.25f, 0.27f, 0.32f);
    public Color startColor = new Color(0.2f, 0.85f, 0.35f);
    public Color goalColor = new Color(1f, 0.85f, 0.15f);
    public Color ballColor = new Color(0.35f, 0.55f, 1f);

    [Header("Ball trail")]
    public float trailTime = 0.38f;
    [Tooltip("Trail width at the ball; tapers to zero along the tail.")]
    public float trailStartWidth = 0.36f;
    public float trailMinVertexDistance = 0.015f;

    [Header("Timing")]
    public float revealHoldSeconds = 2.2f;

    [Header("Camera")]
    public float orthographicPadding = 2.5f;

    [Header("Background")]
    public Color backgroundColorTop = new Color(0.1f, 0.08f, 0.22f);
    public Color backgroundColorBottom = new Color(0.02f, 0.03f, 0.09f);

    int levelIndex = 1;
    bool goalReached;
    bool allLevelsComplete;
    Transform backgroundTf;
    SpriteRenderer backgroundSr;
    AudioSource uiAudio;

    Transform mazeRoot;
    Transform wallsRoot;
    readonly System.Collections.Generic.List<SpriteRenderer> wallSprites = new();
    GameObject ball;
    MazeBallController ballController;
    bool[,] wallMap;
    int startX, startZ, goalX, goalZ;
    int shortestPath;
    int currentGridSize;

    void Start()
    {
        totalLevels = Mathf.Max(1, totalLevels);
        DisableLegacyPlayerIfPresent();
        DisableGroundPlaneIfPresent();
        uiAudio = gameObject.AddComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.spatialBlend = 0f;
        BuildLevel();
    }

    void LateUpdate()
    {
        if (allLevelsComplete || backgroundTf == null) return;
        UpdateBackground();
    }

    void DisableLegacyPlayerIfPresent()
    {
        var legacy = GameObject.Find("Cube");
        if (legacy != null)
            legacy.SetActive(false);
    }

    void DisableGroundPlaneIfPresent()
    {
        var ground = GameObject.Find("Ground");
        if (ground != null)
            ground.SetActive(false);
    }

    int GridSizeForCurrentLevel()
    {
        int g = baseGridSize + (levelIndex - 1) * gridSizePerLevel;
        if ((g & 1) == 0) g++;
        return Mathf.Clamp(g, 7, maxGridSize | 1);
    }

    void BuildLevel()
    {
        if (allLevelsComplete)
            return;

        goalReached = false;
        if (mazeRoot != null)
            Destroy(mazeRoot.gameObject);

        wallSprites.Clear();

        currentGridSize = GridSizeForCurrentLevel();
        var rng = new System.Random(unchecked(levelIndex * 397 ^ 0x5EED));
        var built = MazeGenerator.Build(currentGridSize, rng);
        wallMap = built.walls;
        startX = built.startX;
        startZ = built.startZ;
        goalX = built.goalX;
        goalZ = built.goalZ;
        shortestPath = built.shortestPathLength;

        mazeRoot = new GameObject($"Maze_Level{levelIndex}").transform;
        mazeRoot.SetParent(transform, false);
        wallsRoot = new GameObject("Walls").transform;
        wallsRoot.SetParent(mazeRoot, false);

        for (int gz = 0; gz < currentGridSize; gz++)
        for (int gx = 0; gx < currentGridSize; gx++)
        {
            if (!wallMap[gx, gz]) continue;

            var wall = new GameObject($"Wall_{gx}_{gz}");
            wall.transform.SetParent(wallsRoot, false);
            wall.transform.position = new Vector3(gx, gz, 0f);
            int mazeLayer = LayerMask.NameToLayer("MazeWall");
            wall.layer = mazeLayer >= 0 ? mazeLayer : LayerMask.NameToLayer("Default");
            wall.tag = "MazeWall";

            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = Maze2DVisuals.SquareSprite(wallRevealedColor);
            sr.sortingOrder = 0;
            sr.enabled = false;
            wallSprites.Add(sr);

            var box = wall.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
        }

        CreateMarker("Start", new Vector3(startX, startZ, 0f), startColor, 0.35f, 5);
        CreateGoalMarker(new Vector3(goalX, goalZ, 0f), goalColor, 0.42f, 6);

        EnsureBall();
        var spawn = new Vector2(startX, startZ);
        ball.transform.position = new Vector3(spawn.x, spawn.y, 0f);
        ResetBallPhysics();
        ballController.ResetMoves(spawn);

        WireCamera();
        UpdateBackground();
    }

    void EnsureBall()
    {
        if (ball != null) return;

        ball = new GameObject("Ball");
        ball.tag = "Player";
        ball.transform.SetParent(transform, false);

        var sr = ball.AddComponent<SpriteRenderer>();
        sr.sprite = Maze2DVisuals.CircleSprite(ballColor, 36);
        sr.sortingOrder = 10;

        var cc = ball.AddComponent<CircleCollider2D>();
        cc.radius = 0.28f;

        var rb = ball.AddComponent<Rigidbody2D>();
        rb.mass = 0.9f;
        rb.linearDamping = 10f;
        rb.angularDamping = 8f;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var phys = new PhysicsMaterial2D("BallMaze2D")
        {
            friction = 0.35f,
            bounciness = 0.08f
        };
        cc.sharedMaterial = phys;

        SetupBallTrail(ball, ballColor, trailTime, trailStartWidth, trailMinVertexDistance);

        ballController = ball.AddComponent<MazeBallController>();
        ballController.mazeWallMask = BuildMazeWallLayerMask();
    }

    static LayerMask BuildMazeWallLayerMask()
    {
        int mazeWall = LayerMask.NameToLayer("MazeWall");
        if (mazeWall >= 0)
            return 1 << mazeWall;
        int def = LayerMask.NameToLayer("Default");
        return def >= 0 ? 1 << def : default;
    }

    void SetupBallTrail(GameObject ballGo, Color tint, float time, float startWidth, float minVertexDist)
    {
        var trail = ballGo.AddComponent<TrailRenderer>();
        trail.time = Mathf.Max(0.05f, time);
        trail.minVertexDistance = Mathf.Max(0.001f, minVertexDist);
        trail.numCornerVertices = 3;
        trail.numCapVertices = 2;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.generateLightingData = false;
        trail.sortingOrder = 9;

        float w = Mathf.Max(0.02f, startWidth);
        var wCurve = new AnimationCurve(
            new Keyframe(0f, w),
            new Keyframe(1f, 0f));
        wCurve.preWrapMode = WrapMode.Clamp;
        wCurve.postWrapMode = WrapMode.Clamp;
        trail.widthCurve = wCurve;
        trail.widthMultiplier = 1f;

        var g = new Gradient();
        var bright = tint;
        bright.a = 0.9f;
        var fade = tint;
        fade.a = 0f;
        g.SetKeys(
            new[] { new GradientColorKey(bright, 0f), new GradientColorKey(fade, 1f) },
            new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh != null)
            trail.material = new Material(sh) { color = Color.white };
    }

    void ResetBallPhysics()
    {
        var rb = ball.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.Sleep();

        var trail = ball.GetComponent<TrailRenderer>();
        if (trail != null)
            trail.Clear();
    }

    void CreateMarker(string name, Vector3 pos, Color color, float diameter, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(mazeRoot, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * diameter;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Maze2DVisuals.CircleSprite(color, 32);
        sr.sortingOrder = sortingOrder;
    }

    void CreateGoalMarker(Vector3 pos, Color color, float diameter, int sortingOrder)
    {
        var go = new GameObject("Goal");
        go.tag = "Goal";
        go.transform.SetParent(mazeRoot, false);
        go.transform.position = pos;

        var vis = new GameObject("Graphic");
        vis.transform.SetParent(go.transform, false);
        vis.transform.localScale = Vector3.one * diameter;
        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = Maze2DVisuals.CircleSprite(color, 32);
        sr.sortingOrder = sortingOrder;

        var circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.45f;
        var gt = go.AddComponent<GoalTrigger>();
        gt.Init(this);

        var goalAudio = go.AddComponent<AudioSource>();
        goalAudio.clip = ProceduralAudio.CreateGoalHumClip();
        goalAudio.loop = true;
        goalAudio.spatialBlend = 1f;
        goalAudio.rolloffMode = AudioRolloffMode.Linear;
        goalAudio.minDistance = 1.2f;
        goalAudio.maxDistance = Mathf.Max(18f, currentGridSize * 1.2f);
        goalAudio.volume = 0.55f;
        goalAudio.Play();
    }

    void WireCamera()
    {
        var cam = UnityEngine.Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        float halfExtent = (currentGridSize - 1) * 0.5f + orthographicPadding;
        cam.orthographicSize = Mathf.Max(halfExtent, 6f);
        Vector3 center = new Vector3((currentGridSize - 1) * 0.5f, (currentGridSize - 1) * 0.5f, -10f);
        cam.transform.position = center;
        cam.transform.rotation = Quaternion.identity;

        var follow = cam.GetComponent<Camera.CameraFollowPosition>();
        if (follow != null)
            follow.SetFollowTarget(ball.transform);
    }

    void UpdateBackground()
    {
        var cam = UnityEngine.Camera.main;
        if (cam == null) return;

        if (backgroundTf == null)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(transform, false);
            backgroundTf = go.transform;
            backgroundSr = go.AddComponent<SpriteRenderer>();
            backgroundSr.sprite = Maze2DVisuals.GradientSprite(96, 96, backgroundColorTop, backgroundColorBottom);
            backgroundSr.sortingOrder = -100;
        }

        float h = cam.orthographicSize * 2.4f;
        float w = h * cam.aspect * 1.05f;
        backgroundTf.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        backgroundTf.localScale = new Vector3(w, h, 1f);
    }

    public void OnGoalEntered()
    {
        if (goalReached || allLevelsComplete) return;
        goalReached = true;
        StartCoroutine(GoalSequence());
    }

    IEnumerator GoalSequence()
    {
        foreach (var r in wallSprites)
            if (r != null) r.enabled = true;

        yield return new WaitForSecondsRealtime(revealHoldSeconds);

        bool wasLastLevel = levelIndex >= totalLevels;

        if (wasLastLevel)
        {
            if (uiAudio != null)
                uiAudio.PlayOneShot(ProceduralAudio.CreateFinalVictoryClip(), 1f);

            allLevelsComplete = true;
            if (ball != null)
            {
                var rb2d = ball.GetComponent<Rigidbody2D>();
                if (rb2d != null) rb2d.simulated = false;
                if (ballController != null) ballController.enabled = false;
            }

            yield break;
        }

        foreach (var r in wallSprites)
            if (r != null) r.enabled = false;

        if (uiAudio != null)
            uiAudio.PlayOneShot(ProceduralAudio.CreateLevelVictoryClip(), 0.65f);

        levelIndex++;
        BuildLevel();
    }

    void OnGUI()
    {
        GUIStyle s = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft };
        string line4 = allLevelsComplete
            ? "You cleared every maze!"
            : "WASD / arrows — 2D roll; maze reveals at the goal.";
        string text =
            (allLevelsComplete ? $"<color=#FFD700>ALL {totalLevels} LEVELS COMPLETE!</color>\n\n" : "") +
            $"Level {levelIndex} / {totalLevels}\n" +
            $"Moves: {(ballController != null ? ballController.Moves : 0)}\n" +
            $"Shortest path (grid steps): {shortestPath}\n" +
            line4;
        var box = new GUIStyle(s) { richText = true };
        GUI.Box(new Rect(12, 12, 420, allLevelsComplete ? 140 : 110), text, box);
    }
}

static class Maze2DVisuals
{
    public static Sprite GradientSprite(int width, int height, Color top, Color bottom)
    {
        width = Mathf.Clamp(width, 8, 256);
        height = Mathf.Clamp(height, 8, 256);
        var t = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            float k = height > 1 ? y / (float)(height - 1) : 0f;
            Color c = Color.Lerp(bottom, top, k);
            for (int x = 0; x < width; x++)
                t.SetPixel(x, y, c);
        }

        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(t, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
    }

    public static Sprite SquareSprite(Color color)
    {
        const int n = 4;
        var t = new Texture2D(n, n);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
            t.SetPixel(x, y, color);
        t.Apply();
        t.filterMode = FilterMode.Point;
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }

    public static Sprite CircleSprite(Color color, int resolution)
    {
        resolution = Mathf.Clamp(resolution, 8, 64);
        var t = new Texture2D(resolution, resolution);
        float r = resolution / 2f - 0.5f;
        var c = new Vector2(resolution / 2f, resolution / 2f);
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            t.SetPixel(x, y, d <= r ? color : Color.clear);
        }

        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        return Sprite.Create(t, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }
}
