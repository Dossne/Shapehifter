using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridGameManager : MonoBehaviour
{
    private enum TileType
    {
        Floor,
        Wall,
        Dirt,
        Water
    }

    private enum Form
    {
        Chameleon,
        Frog,
        Gorilla,
        Mole
    }

    [Header("Level Setup")]
    [SerializeField] private int shapeshiftsPerLevel = 5;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private string[] levelNames = { "level1", "level2", "level3", "level4" };

    private readonly Dictionary<Vector2Int, TileType> tiles = new Dictionary<Vector2Int, TileType>();
    private readonly Dictionary<Vector2Int, GameObject> boulders = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> tokens = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> keys = new Dictionary<Vector2Int, GameObject>();

    private GameObject doorObject;
    private Vector2Int doorPosition;
    private bool doorOpen;

    private GameObject playerObject;
    private Vector2Int playerPosition;

    private HashSet<Form> availableForms = new HashSet<Form>();
    private readonly List<Form> formOrder = new List<Form> { Form.Chameleon, Form.Frog, Form.Gorilla, Form.Mole };
    private Form currentForm = Form.Chameleon;

    private int shapeshiftsRemaining;
    private bool hasKey;

    private int currentLevelIndex;
    private GameObject levelRoot;

    private Text hudText;
    private Sprite squareSprite;

    private int levelWidth;
    private int levelHeight;

    private void Awake()
    {
        CreateSprite();
        CreateHud();
        LoadLevel(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadLevel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryCycleForm();
        }

        Vector2Int direction = GetMoveDirection();
        if (direction != Vector2Int.zero)
        {
            AttemptMove(direction);
        }
    }

    private void CreateSprite()
    {
        if (squareSprite != null)
        {
            return;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void CreateHud()
    {
        if (hudText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("HudText");
        textObject.transform.SetParent(canvasObject.transform, false);

        hudText = textObject.AddComponent<Text>();
        hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hudText.fontSize = 18;
        hudText.alignment = TextAnchor.UpperLeft;
        hudText.color = Color.white;

        RectTransform rectTransform = hudText.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(10f, -10f);
        rectTransform.sizeDelta = new Vector2(400f, 80f);
    }

    private void LoadLevel(int index)
    {
        currentLevelIndex = Mathf.Clamp(index, 0, levelNames.Length - 1);
        shapeshiftsRemaining = shapeshiftsPerLevel;
        hasKey = false;
        doorOpen = false;
        currentForm = Form.Chameleon;
        availableForms = new HashSet<Form> { Form.Chameleon };

        if (levelRoot != null)
        {
            Destroy(levelRoot);
        }

        tiles.Clear();
        boulders.Clear();
        tokens.Clear();
        keys.Clear();
        doorObject = null;
        levelRoot = new GameObject("LevelRoot");

        TextAsset levelAsset = Resources.Load<TextAsset>("Levels/" + levelNames[currentLevelIndex]);
        if (levelAsset == null)
        {
            Debug.LogError("Missing level: " + levelNames[currentLevelIndex]);
            return;
        }

        string[] lines = levelAsset.text.Replace("\r", string.Empty).Split('\n');
        List<string> rows = new List<string>();
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                rows.Add(line.TrimEnd());
            }
        }

        levelHeight = rows.Count;
        levelWidth = 0;
        foreach (string row in rows)
        {
            if (row.Length > levelWidth)
            {
                levelWidth = row.Length;
            }
        }

        for (int y = 0; y < rows.Count; y++)
        {
            string row = rows[y];
            for (int x = 0; x < row.Length; x++)
            {
                char cell = row[x];
                Vector2Int gridPosition = new Vector2Int(x, levelHeight - 1 - y);
                ParseCell(cell, gridPosition);
            }
        }

        UpdateHud();
        UpdateCamera();
    }

    private void ParseCell(char cell, Vector2Int position)
    {
        switch (cell)
        {
            case '#':
                tiles[position] = TileType.Wall;
                CreateTile(position, new Color(0.25f, 0.25f, 0.25f));
                break;
            case 'd':
                tiles[position] = TileType.Dirt;
                CreateTile(position, new Color(0.55f, 0.35f, 0.2f));
                break;
            case 'w':
                tiles[position] = TileType.Water;
                CreateTile(position, new Color(0.2f, 0.45f, 0.8f));
                break;
            case 'b':
                CreateFloor(position);
                CreateObject(position, new Color(0.35f, 0.35f, 0.35f), boulders);
                break;
            case 'k':
                CreateFloor(position);
                CreateObject(position, new Color(1f, 0.9f, 0.1f), keys);
                break;
            case 'D':
                CreateFloor(position);
                doorPosition = position;
                doorObject = CreateSingleObject(position, new Color(0.8f, 0.2f, 0.2f));
                break;
            case 'P':
                CreateFloor(position);
                playerPosition = position;
                CreatePlayer(position);
                break;
            case 'F':
                CreateFloor(position);
                CreateToken(position, Form.Frog, new Color(0.2f, 0.8f, 0.2f));
                break;
            case 'G':
                CreateFloor(position);
                CreateToken(position, Form.Gorilla, new Color(0.55f, 0.4f, 0.2f));
                break;
            case 'M':
                CreateFloor(position);
                CreateToken(position, Form.Mole, new Color(0.5f, 0.3f, 0.4f));
                break;
            case '.':
            default:
                CreateFloor(position);
                break;
        }
    }

    private void CreateFloor(Vector2Int position)
    {
        tiles[position] = TileType.Floor;
        CreateTile(position, new Color(0.75f, 0.75f, 0.75f));
    }

    private void CreateTile(Vector2Int position, Color color)
    {
        GameObject tile = new GameObject("Tile_" + position.x + "_" + position.y);
        tile.transform.SetParent(levelRoot.transform, false);
        tile.transform.localPosition = new Vector3(position.x * tileSize, position.y * tileSize, 0f);
        SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = 0;
    }

    private void CreateObject(Vector2Int position, Color color, Dictionary<Vector2Int, GameObject> bucket)
    {
        GameObject obj = CreateSingleObject(position, color);
        bucket[position] = obj;
    }

    private void CreateToken(Vector2Int position, Form form, Color color)
    {
        GameObject obj = CreateSingleObject(position, color);
        tokens[position] = obj;
        obj.name = form + "Token";
    }

    private GameObject CreateSingleObject(Vector2Int position, Color color)
    {
        GameObject obj = new GameObject("Object_" + position.x + "_" + position.y);
        obj.transform.SetParent(levelRoot.transform, false);
        obj.transform.localPosition = new Vector3(position.x * tileSize, position.y * tileSize, -0.1f);
        obj.transform.localScale = Vector3.one * 0.9f;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = 1;
        return obj;
    }

    private void CreatePlayer(Vector2Int position)
    {
        if (playerObject == null)
        {
            playerObject = new GameObject("Player");
            SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.sortingOrder = 2;
            playerObject.transform.localScale = Vector3.one * 0.85f;
        }

        playerObject.transform.SetParent(levelRoot.transform, false);
        playerObject.transform.localPosition = new Vector3(position.x * tileSize, position.y * tileSize, -0.2f);
        UpdatePlayerColor();
    }

    private void UpdatePlayerPosition(Vector2Int position)
    {
        playerPosition = position;
        if (playerObject != null)
        {
            playerObject.transform.localPosition = new Vector3(position.x * tileSize, position.y * tileSize, -0.2f);
        }
    }

    private void UpdatePlayerColor()
    {
        if (playerObject == null)
        {
            return;
        }

        Color color = Color.green;
        switch (currentForm)
        {
            case Form.Chameleon:
                color = new Color(0.2f, 0.7f, 0.3f);
                break;
            case Form.Frog:
                color = new Color(0.2f, 0.9f, 0.2f);
                break;
            case Form.Gorilla:
                color = new Color(0.4f, 0.3f, 0.2f);
                break;
            case Form.Mole:
                color = new Color(0.45f, 0.2f, 0.3f);
                break;
        }

        playerObject.GetComponent<SpriteRenderer>().color = color;
    }

    private Vector2Int GetMoveDirection()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            return Vector2Int.up;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            return Vector2Int.down;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            return Vector2Int.left;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            return Vector2Int.right;
        }

        return Vector2Int.zero;
    }

    private void AttemptMove(Vector2Int direction)
    {
        Vector2Int target = playerPosition + direction;
        if (!IsInLevel(target))
        {
            return;
        }

        if (currentForm == Form.Frog && IsWater(target))
        {
            Vector2Int landing = playerPosition + direction * 2;
            if (IsInLevel(landing) && !IsBlockedByDoor(landing) && IsTileWalkableForForm(landing, currentForm) && !boulders.ContainsKey(landing))
            {
                UpdatePlayerPosition(landing);
                ResolveLanding();
            }
            return;
        }

        if (IsBlockedByDoor(target))
        {
            return;
        }

        if (boulders.ContainsKey(target))
        {
            if (currentForm != Form.Gorilla)
            {
                return;
            }

            Vector2Int pushTarget = target + direction;
            if (!IsInLevel(pushTarget))
            {
                return;
            }

            if (boulders.ContainsKey(pushTarget) || keys.ContainsKey(pushTarget) || tokens.ContainsKey(pushTarget) || (doorObject != null && doorPosition == pushTarget))
            {
                return;
            }

            if (!IsTileWalkableForBoulder(pushTarget))
            {
                return;
            }

            GameObject boulder = boulders[target];
            boulders.Remove(target);
            boulder.transform.localPosition = new Vector3(pushTarget.x * tileSize, pushTarget.y * tileSize, -0.1f);
            boulders[pushTarget] = boulder;

            UpdatePlayerPosition(target);
            ResolveLanding();
            return;
        }

        if (!IsTileWalkableForForm(target, currentForm))
        {
            return;
        }

        UpdatePlayerPosition(target);
        ResolveLanding();
    }

    private void ResolveLanding()
    {
        if (keys.TryGetValue(playerPosition, out GameObject keyObj))
        {
            keys.Remove(playerPosition);
            Destroy(keyObj);
            hasKey = true;
            doorOpen = true;
            UpdateDoorVisual();
        }

        if (tokens.TryGetValue(playerPosition, out GameObject tokenObj))
        {
            Form unlocked = GetTokenForm(tokenObj.name);
            availableForms.Add(unlocked);
            tokens.Remove(playerPosition);
            Destroy(tokenObj);
        }

        if (doorObject != null && doorOpen && playerPosition == doorPosition)
        {
            LoadNextLevel();
            return;
        }

        UpdateHud();
    }

    private Form GetTokenForm(string tokenName)
    {
        if (tokenName.Contains(Form.Frog.ToString()))
        {
            return Form.Frog;
        }

        if (tokenName.Contains(Form.Gorilla.ToString()))
        {
            return Form.Gorilla;
        }

        if (tokenName.Contains(Form.Mole.ToString()))
        {
            return Form.Mole;
        }

        return Form.Chameleon;
    }

    private void TryCycleForm()
    {
        if (shapeshiftsRemaining <= 0)
        {
            return;
        }

        int currentIndex = formOrder.IndexOf(currentForm);
        int nextIndex = currentIndex;

        for (int i = 0; i < formOrder.Count; i++)
        {
            nextIndex = (nextIndex + 1) % formOrder.Count;
            Form candidate = formOrder[nextIndex];
            if (availableForms.Contains(candidate))
            {
                if (candidate != currentForm)
                {
                    currentForm = candidate;
                    shapeshiftsRemaining--;
                    UpdatePlayerColor();
                    UpdateHud();
                }
                break;
            }
        }
    }

    private void UpdateHud()
    {
        if (hudText != null)
        {
            hudText.text = $"Form: {currentForm} | Shifts: {shapeshiftsRemaining}";
        }
    }

    private void UpdateDoorVisual()
    {
        if (doorObject == null)
        {
            return;
        }

        SpriteRenderer renderer = doorObject.GetComponent<SpriteRenderer>();
        renderer.color = doorOpen ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
    }

    private void ReloadLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    private void LoadNextLevel()
    {
        int nextIndex = (currentLevelIndex + 1) % levelNames.Length;
        LoadLevel(nextIndex);
    }

    private bool IsInLevel(Vector2Int position)
    {
        return tiles.ContainsKey(position);
    }

    private bool IsWater(Vector2Int position)
    {
        return tiles.TryGetValue(position, out TileType type) && type == TileType.Water;
    }

    private bool IsTileWalkableForForm(Vector2Int position, Form form)
    {
        if (!tiles.TryGetValue(position, out TileType type))
        {
            return false;
        }

        if (type == TileType.Wall)
        {
            return false;
        }

        if (type == TileType.Dirt)
        {
            return form == Form.Mole;
        }

        if (type == TileType.Water)
        {
            return false;
        }

        return true;
    }

    private bool IsTileWalkableForBoulder(Vector2Int position)
    {
        return tiles.TryGetValue(position, out TileType type) && type == TileType.Floor;
    }

    private bool IsBlockedByDoor(Vector2Int position)
    {
        if (doorObject == null || doorOpen)
        {
            return false;
        }

        return position == doorPosition;
    }

    private void UpdateCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        float centerX = (levelWidth - 1) * tileSize * 0.5f;
        float centerY = (levelHeight - 1) * tileSize * 0.5f;
        camera.transform.position = new Vector3(centerX, centerY, -10f);
        float size = Mathf.Max(levelWidth, levelHeight) * 0.5f + 1f;
        camera.orthographicSize = size;
    }
}
