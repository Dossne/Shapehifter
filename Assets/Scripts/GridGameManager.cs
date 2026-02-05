using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridGameManager : MonoBehaviour
{
    private static class SpriteCatalog
    {
        public const string TilesFolder = "Art/Tiles/";
        public const string ObjectsFolder = "Art/Objects/";
        public const string TokensFolder = "Art/Tokens/";

        public const string TileFloor = "floor";
        public const string TileWaterShallow = "water_shallow";
        public const string TileWaterDeep = "water_deep";
        public const string TileStoneWall = "stone_wall";
        public const string TileDirtWall = "dirt_wall";

        public const string ObjectBoulder = "boulder";
        public const string ObjectKey = "key";
        public const string ObjectDoor = "door";
        public const string ObjectDoorOpened = "door_opened";
        public const string ObjectSpikes = "spikes";

        public const string TokenChameleon = "chameleon";
        public const string TokenFrog = "frog";
        public const string TokenGorilla = "gorilla";
        public const string TokenMole = "mole";
        public const string TokenBubble = "bubble";

        public static readonly Dictionary<string, string> TileSprites = new Dictionary<string, string>
        {
            { TileFloor, TileFloor },
            { TileWaterShallow, TileWaterShallow },
            { TileWaterDeep, TileWaterDeep },
            { TileStoneWall, TileStoneWall },
            { TileDirtWall, TileDirtWall }
        };

        public static readonly Dictionary<string, string> ObjectSprites = new Dictionary<string, string>
        {
            { ObjectBoulder, ObjectBoulder },
            { ObjectKey, ObjectKey },
            { ObjectDoor, ObjectDoor },
            { ObjectDoorOpened, ObjectDoorOpened },
            { ObjectSpikes, ObjectSpikes }
        };

        public static readonly Dictionary<string, string> TokenSprites = new Dictionary<string, string>
        {
            { TokenChameleon, TokenChameleon },
            { TokenFrog, TokenFrog },
            { TokenGorilla, TokenGorilla },
            { TokenMole, TokenMole }
        };

        public static readonly Dictionary<TileType, string> TileTypeToKey = new Dictionary<TileType, string>
        {
            { TileType.Floor, TileFloor },
            { TileType.Wall, TileStoneWall },
            { TileType.Dirt, TileDirtWall },
            { TileType.Water, TileWaterShallow }
        };

        public static readonly Dictionary<Form, string> FormToTokenKey = new Dictionary<Form, string>
        {
            { Form.Chameleon, TokenChameleon },
            { Form.Frog, TokenFrog },
            { Form.Gorilla, TokenGorilla },
            { Form.Mole, TokenMole }
        };
    }

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

    private readonly List<TextAsset> levelAssets = new List<TextAsset>();

    private readonly Dictionary<Vector2Int, TileType> tiles = new Dictionary<Vector2Int, TileType>();
    private readonly Dictionary<Vector2Int, GameObject> boulders = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> tokens = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> keys = new Dictionary<Vector2Int, GameObject>();

    private GameObject doorObject;
    private Vector2Int doorPosition;
    private bool doorOpen;
    private Coroutine doorBounceRoutine;

    private GameObject playerObject;
    private Vector2Int playerPosition;

    private HashSet<Form> availableForms = new HashSet<Form>();
    private readonly List<Form> formOrder = new List<Form> { Form.Chameleon, Form.Frog, Form.Gorilla, Form.Mole };
    private Form currentForm = Form.Chameleon;

    private int shapeshiftsRemaining;
    private bool hasKey;

    private int currentLevelIndex;
    private GameObject levelRoot;

    private TextMeshProUGUI tutorialText;
    private TextMeshProUGUI statusText;
    private Canvas hudCanvas;
    private RectTransform hudRectTransform;
    private RectTransform hudSafeAreaRect;
    private GameObject tutorialPanel;
    private GameObject statusPanel;
    private VerticalLayoutGroup hudLayoutGroup;
    private RectOffset hudPadding;
    private RectOffset hudPaddingCollapsed;
    private const float HudSpacing = 12f;
    private MobileInputController mobileInput;
    private Sprite squareSprite;
    private string tutorialLine = string.Empty;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private int levelWidth;
    private int levelHeight;

    private void Awake()
    {
        hudPadding = new RectOffset(40, 40, 40, 20);
        hudPaddingCollapsed = new RectOffset(0, 0, 0, 0);
        ConfigureOrientation();
        CreateSprite();
        CreateHud();
        EnsureMobileInput();
        DiscoverLevels();
        LoadLevel(0);
    }

    private void Update()
    {
        if (hudSafeAreaRect != null && HasSafeAreaChanged())
        {
            ApplySafeArea();
        }

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

    private void ConfigureOrientation()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
    }

    private void CreateHud()
    {
        if (statusText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("HUD");
        hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject safeAreaObject = new GameObject("HudSafeArea");
        safeAreaObject.transform.SetParent(canvasObject.transform, false);
        hudSafeAreaRect = safeAreaObject.AddComponent<RectTransform>();

        GameObject hudContainer = new GameObject("HudContainer");
        hudContainer.transform.SetParent(safeAreaObject.transform, false);
        hudRectTransform = hudContainer.AddComponent<RectTransform>();
        hudRectTransform.anchorMin = new Vector2(0f, 1f);
        hudRectTransform.anchorMax = new Vector2(1f, 1f);
        hudRectTransform.pivot = new Vector2(0.5f, 1f);
        hudRectTransform.anchoredPosition = Vector2.zero;
        hudRectTransform.sizeDelta = Vector2.zero;

        hudLayoutGroup = hudContainer.AddComponent<VerticalLayoutGroup>();
        hudLayoutGroup.padding = hudPadding;
        hudLayoutGroup.spacing = HudSpacing;
        hudLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        hudLayoutGroup.childControlWidth = true;
        hudLayoutGroup.childControlHeight = true;
        hudLayoutGroup.childForceExpandWidth = true;
        hudLayoutGroup.childForceExpandHeight = false;

        ContentSizeFitter containerFitter = hudContainer.AddComponent<ContentSizeFitter>();
        containerFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        tutorialPanel = new GameObject("TutorialPanel");
        tutorialPanel.transform.SetParent(hudContainer.transform, false);
        Image tutorialBackground = tutorialPanel.AddComponent<Image>();
        tutorialBackground.sprite = squareSprite;
        tutorialBackground.color = new Color(0f, 0f, 0f, 0.6f);
        tutorialBackground.raycastTarget = false;

        VerticalLayoutGroup tutorialLayout = tutorialPanel.AddComponent<VerticalLayoutGroup>();
        tutorialLayout.padding = new RectOffset(24, 24, 20, 20);
        tutorialLayout.childAlignment = TextAnchor.UpperLeft;
        tutorialLayout.childControlWidth = true;
        tutorialLayout.childControlHeight = true;
        tutorialLayout.childForceExpandWidth = true;
        tutorialLayout.childForceExpandHeight = false;

        ContentSizeFitter tutorialFitter = tutorialPanel.AddComponent<ContentSizeFitter>();
        tutorialFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        tutorialFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject tutorialTextObject = new GameObject("TutorialText");
        tutorialTextObject.transform.SetParent(tutorialPanel.transform, false);
        tutorialText = tutorialTextObject.AddComponent<TextMeshProUGUI>();
        tutorialText.fontSize = 64f;
        tutorialText.enableAutoSizing = true;
        tutorialText.fontSizeMin = 48f;
        tutorialText.fontSizeMax = 72f;
        tutorialText.alignment = TextAlignmentOptions.TopLeft;
        tutorialText.color = Color.white;
        tutorialText.enableWordWrapping = true;
        tutorialText.raycastTarget = false;

        Outline tutorialOutline = tutorialTextObject.AddComponent<Outline>();
        tutorialOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        tutorialOutline.effectDistance = new Vector2(2f, -2f);

        statusPanel = new GameObject("StatusPanel");
        statusPanel.transform.SetParent(hudContainer.transform, false);
        Image statusBackground = statusPanel.AddComponent<Image>();
        statusBackground.sprite = squareSprite;
        statusBackground.color = new Color(0f, 0f, 0f, 0.55f);
        statusBackground.raycastTarget = false;

        VerticalLayoutGroup statusLayout = statusPanel.AddComponent<VerticalLayoutGroup>();
        statusLayout.padding = new RectOffset(20, 20, 16, 16);
        statusLayout.childAlignment = TextAnchor.UpperLeft;
        statusLayout.childControlWidth = true;
        statusLayout.childControlHeight = true;
        statusLayout.childForceExpandWidth = true;
        statusLayout.childForceExpandHeight = false;

        ContentSizeFitter statusFitter = statusPanel.AddComponent<ContentSizeFitter>();
        statusFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        statusFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject statusObject = new GameObject("StatusText");
        statusObject.transform.SetParent(statusPanel.transform, false);
        statusText = statusObject.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 36f;
        statusText.alignment = TextAlignmentOptions.TopLeft;
        statusText.color = Color.white;
        statusText.enableWordWrapping = true;
        statusText.raycastTarget = false;

        Outline statusOutline = statusObject.AddComponent<Outline>();
        statusOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        statusOutline.effectDistance = new Vector2(1.5f, -1.5f);

        ApplySafeArea();
    }

    private void EnsureMobileInput()
    {
        if (mobileInput == null)
        {
            mobileInput = GetComponent<MobileInputController>();
            if (mobileInput == null)
            {
                mobileInput = gameObject.AddComponent<MobileInputController>();
            }
        }

        mobileInput.Initialize(this, hudRectTransform, hudCanvas);
    }

    private void DiscoverLevels()
    {
        levelAssets.Clear();
        TextAsset[] discovered = Resources.LoadAll<TextAsset>("Levels");
        if (discovered == null || discovered.Length == 0)
        {
            Debug.LogError("No levels found in Resources/Levels.");
            return;
        }

        levelAssets.AddRange(discovered);
        levelAssets.Sort(CompareLevelAssets);

        List<string> levelNames = new List<string>();
        foreach (TextAsset asset in levelAssets)
        {
            if (asset != null)
            {
                levelNames.Add(asset.name);
            }
        }

        Debug.Log("Discovered levels: " + string.Join(", ", levelNames));
    }

    private int CompareLevelAssets(TextAsset left, TextAsset right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return CompareLevelNames(leftName, rightName);
    }

    private int CompareLevelNames(string left, string right)
    {
        bool leftHasNumber = TryExtractNumber(left, out int leftNumber);
        bool rightHasNumber = TryExtractNumber(right, out int rightNumber);

        if (leftHasNumber && rightHasNumber)
        {
            int numberComparison = leftNumber.CompareTo(rightNumber);
            if (numberComparison != 0)
            {
                return numberComparison;
            }
        }
        else if (leftHasNumber != rightHasNumber)
        {
            return leftHasNumber ? -1 : 1;
        }

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryExtractNumber(string name, out int number)
    {
        number = 0;
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        for (int i = 0; i < name.Length; i++)
        {
            if (!char.IsDigit(name[i]))
            {
                continue;
            }

            int start = i;
            while (i < name.Length && char.IsDigit(name[i]))
            {
                i++;
            }

            string digits = name.Substring(start, i - start);
            return int.TryParse(digits, out number);
        }

        return false;
    }

    private void LoadLevel(int index)
    {
        if (levelAssets.Count == 0)
        {
            Debug.LogError("No levels available to load.");
            return;
        }

        int normalizedIndex = Mod(index, levelAssets.Count);
        LoadLevelInternal(normalizedIndex, 0);
    }

    private void LoadLevelInternal(int index, int attempts)
    {
        if (attempts >= levelAssets.Count)
        {
            Debug.LogError("No valid levels could be loaded.");
            return;
        }

        TextAsset levelAsset = levelAssets[index];
        if (levelAsset == null)
        {
            Debug.LogError("Missing level asset at index " + index + ".");
            LoadLevelInternal(Mod(index + 1, levelAssets.Count), attempts + 1);
            return;
        }

        if (!TryBuildLevel(levelAsset))
        {
            Debug.LogError("Malformed level skipped: " + levelAsset.name);
            LoadLevelInternal(Mod(index + 1, levelAssets.Count), attempts + 1);
            return;
        }

        currentLevelIndex = index;
    }

    private bool TryBuildLevel(TextAsset levelAsset)
    {
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

        string[] lines = levelAsset.text.Replace("\r", string.Empty).Split('\n');
        List<string> rows = new List<string>();
        tutorialLine = lines.Length > 0 ? lines[0].TrimEnd() : string.Empty;
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (!string.IsNullOrWhiteSpace(line))
            {
                rows.Add(line.TrimEnd());
            }
        }

        if (rows.Count == 0)
        {
            Debug.LogError("Level has no grid rows: " + levelAsset.name);
            return false;
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

        bool playerFound = false;
        for (int y = 0; y < rows.Count; y++)
        {
            string row = rows[y];
            for (int x = 0; x < row.Length; x++)
            {
                char cell = row[x];
                Vector2Int gridPosition = new Vector2Int(x, -y);
                if (cell == 'P')
                {
                    playerFound = true;
                }
                ParseCell(cell, gridPosition);
            }
        }

        if (!playerFound)
        {
            Debug.LogError("Level has no player start: " + levelAsset.name);
            return false;
        }

        UpdateHud();
        return true;
    }

    private void ParseCell(char cell, Vector2Int position)
    {
        switch (cell)
        {
            case '#':
                tiles[position] = TileType.Wall;
                CreateTile(position, TileType.Wall, new Color(0.25f, 0.25f, 0.25f));
                break;
            case 'd':
                tiles[position] = TileType.Dirt;
                CreateTile(position, TileType.Dirt, new Color(0.55f, 0.35f, 0.2f));
                break;
            case 'w':
                tiles[position] = TileType.Water;
                CreateTile(position, TileType.Water, new Color(0.2f, 0.45f, 0.8f));
                break;
            case 'b':
                CreateFloor(position);
                CreateObject(position, SpriteCatalog.ObjectBoulder, new Color(0.35f, 0.35f, 0.35f), boulders);
                break;
            case 'k':
                CreateFloor(position);
                CreateObject(position, SpriteCatalog.ObjectKey, new Color(1f, 0.9f, 0.1f), keys);
                break;
            case 'D':
                CreateFloor(position);
                doorPosition = position;
                doorObject = CreateSingleObject(position, SpriteCatalog.ObjectDoor, Color.white);
                UpdateDoorVisual();
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
        CreateTile(position, TileType.Floor, new Color(0.75f, 0.75f, 0.75f));
    }

    private void CreateTile(Vector2Int position, TileType type, Color color)
    {
        GameObject tile = new GameObject("Tile_" + position.x + "_" + position.y);
        tile.transform.SetParent(levelRoot.transform, false);
        tile.transform.localPosition = new Vector3(position.x * tileSize, position.y * tileSize, 0f);
        SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
        string spriteKey = SpriteCatalog.TileTypeToKey[type];
        string spriteName = SpriteCatalog.TileSprites[spriteKey];
        ApplySpriteOrFallback(renderer, SpriteCatalog.TilesFolder, spriteName, color);
        renderer.sortingOrder = 0;
    }

    private void CreateObject(Vector2Int position, string spriteKey, Color color, Dictionary<Vector2Int, GameObject> bucket)
    {
        GameObject obj = CreateSingleObject(position, spriteKey, color);
        bucket[position] = obj;
    }

    private void CreateToken(Vector2Int position, Form form, Color color)
    {
        string spriteKey = SpriteCatalog.FormToTokenKey[form];
        string spriteName = SpriteCatalog.TokenSprites[spriteKey];
        GameObject obj = new GameObject("Object_" + position.x + "_" + position.y);
        obj.transform.SetParent(levelRoot.transform, false);
        obj.transform.localPosition = new Vector3(position.x * tileSize, position.y * tileSize, -0.1f);
        obj.transform.localScale = Vector3.one;

        Sprite iconSprite = LoadSprite(SpriteCatalog.TokensFolder, spriteName);
        Color iconColor = iconSprite != null ? Color.white : color;
        if (iconSprite == null)
        {
            iconSprite = squareSprite;
        }

        Sprite bubbleSprite = LoadSprite(SpriteCatalog.TokensFolder, SpriteCatalog.TokenBubble);
        TokenVisual tokenVisual = obj.AddComponent<TokenVisual>();
        tokenVisual.Configure(iconSprite, iconColor, bubbleSprite);
        tokens[position] = obj;
        obj.name = form + "Token";
    }

    private GameObject CreateSingleObject(Vector2Int position, string spriteKey, Color color)
    {
        return CreateSingleObject(position, spriteKey, color, SpriteCatalog.ObjectsFolder, SpriteCatalog.ObjectSprites);
    }

    private GameObject CreateSingleObject(
        Vector2Int position,
        string spriteKey,
        Color color,
        string folder,
        Dictionary<string, string> spriteTable)
    {
        GameObject obj = new GameObject("Object_" + position.x + "_" + position.y);
        obj.transform.SetParent(levelRoot.transform, false);
        obj.transform.localPosition = new Vector3(position.x * tileSize, position.y * tileSize, -0.1f);
        obj.transform.localScale = Vector3.one * 0.9f;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        string spriteName = spriteTable[spriteKey];
        ApplySpriteOrFallback(renderer, folder, spriteName, color);
        renderer.sortingOrder = 1;
        return obj;
    }

    private void CreatePlayer(Vector2Int position)
    {
        if (playerObject == null)
        {
            playerObject = new GameObject("Player");
            SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 2;
            playerObject.transform.localScale = Vector3.one * 0.85f;
            playerObject.AddComponent<ShapeshiftVfxController>();
        }

        playerObject.transform.SetParent(levelRoot.transform, false);
        playerObject.transform.localPosition = new Vector3(position.x * tileSize, position.y * tileSize, -0.2f);
        UpdatePlayerVisual();
    }

    private void UpdatePlayerPosition(Vector2Int position)
    {
        playerPosition = position;
        if (playerObject != null)
        {
            playerObject.transform.localPosition = new Vector3(position.x * tileSize, position.y * tileSize, -0.2f);
        }
    }

    private void UpdatePlayerVisual()
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

        string spriteKey = SpriteCatalog.FormToTokenKey[currentForm];
        SpriteRenderer renderer = playerObject.GetComponent<SpriteRenderer>();
        ApplySpriteOrFallback(renderer, SpriteCatalog.TokensFolder, SpriteCatalog.TokenSprites[spriteKey], color);
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
            if (!doorOpen)
            {
                doorOpen = true;
                UpdateDoorVisual();
                PlayDoorUnlockVfx();
            }
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
        if (!TryGetNextAvailableForm(out Form nextForm))
        {
            return;
        }

        SpriteRenderer renderer = playerObject != null ? playerObject.GetComponent<SpriteRenderer>() : null;
        Sprite previousSprite = renderer != null ? renderer.sprite : null;
        Color previousColor = renderer != null ? renderer.color : Color.white;

        currentForm = nextForm;
        shapeshiftsRemaining--;
        UpdatePlayerVisual();

        if (renderer != null)
        {
            ShapeshiftVfxController vfx = playerObject.GetComponent<ShapeshiftVfxController>();
            if (vfx != null)
            {
                vfx.Play(previousSprite, previousColor, renderer.sprite, renderer.color);
            }
        }
        UpdateHud();
    }

    private bool TryGetNextAvailableForm(out Form nextForm)
    {
        nextForm = currentForm;

        if (shapeshiftsRemaining <= 0 || availableForms.Count <= 1)
        {
            return false;
        }

        int currentIndex = formOrder.IndexOf(currentForm);
        int nextIndex = currentIndex;

        for (int i = 0; i < formOrder.Count; i++)
        {
            nextIndex = (nextIndex + 1) % formOrder.Count;
            Form candidate = formOrder[nextIndex];
            if (availableForms.Contains(candidate) && candidate != currentForm)
            {
                nextForm = candidate;
                return true;
            }
        }

        return false;
    }

    private void UpdateHud()
    {
        string statusLine = string.Empty;
        if (statusText != null)
        {
            statusLine = $"Form: {currentForm} | Shifts: {shapeshiftsRemaining}";
            if (TryGetNextAvailableForm(out Form nextForm))
            {
                statusLine += $" | {nextForm} - Tap to Change";
            }

            statusText.text = statusLine;
        }

        if (tutorialText != null)
        {
            tutorialText.text = tutorialLine;
        }

        bool showTutorial = !string.IsNullOrWhiteSpace(tutorialLine);
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(showTutorial);
        }

        bool showStatus = !string.IsNullOrWhiteSpace(statusLine);
        if (statusPanel != null)
        {
            statusPanel.SetActive(showStatus);
        }

        if (hudLayoutGroup != null)
        {
            bool showHud = showTutorial || showStatus;
            hudLayoutGroup.padding = showHud ? hudPadding : hudPaddingCollapsed;
            hudLayoutGroup.spacing = showHud ? HudSpacing : 0f;
        }

        UpdateCamera();
    }

    private bool HasSafeAreaChanged()
    {
        if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
        {
            return true;
        }

        Rect safeArea = Screen.safeArea;
        return safeArea != lastSafeArea;
    }

    private void ApplySafeArea()
    {
        if (hudSafeAreaRect == null)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= screenSize.x;
        anchorMin.y /= screenSize.y;
        anchorMax.x /= screenSize.x;
        anchorMax.y /= screenSize.y;

        hudSafeAreaRect.anchorMin = anchorMin;
        hudSafeAreaRect.anchorMax = anchorMax;
        hudSafeAreaRect.offsetMin = Vector2.zero;
        hudSafeAreaRect.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        UpdateCamera();
    }

    public void RequestMove(Vector2Int direction)
    {
        if (direction != Vector2Int.zero)
        {
            AttemptMove(direction);
        }
    }

    public void RequestCycleForm()
    {
        TryCycleForm();
    }

    private void UpdateDoorVisual()
    {
        if (doorObject == null)
        {
            return;
        }

        SpriteRenderer renderer = doorObject.GetComponent<SpriteRenderer>();
        renderer.sprite = ResolveDoorSprite(doorOpen);
        renderer.color = Color.white;
    }

    private Sprite ResolveDoorSprite(bool open)
    {
        if (open)
        {
            Sprite opened = LoadSprite(SpriteCatalog.ObjectsFolder, SpriteCatalog.ObjectDoorOpened);
            if (opened != null)
            {
                return opened;
            }
        }

        Sprite closed = LoadSprite(SpriteCatalog.ObjectsFolder, SpriteCatalog.ObjectDoor);
        return closed != null ? closed : squareSprite;
    }

    private void PlayDoorUnlockVfx()
    {
        if (doorObject == null)
        {
            return;
        }

        Transform doorTransform = doorObject.transform;
        Vector3 worldPosition = doorTransform.position;

        GameObject vfxObject = new GameObject("DoorUnlockVfx");
        vfxObject.transform.SetParent(levelRoot.transform, false);
        vfxObject.transform.position = worldPosition + new Vector3(0f, tileSize * 0.15f, 0f);

        ParticleSystem particles = vfxObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.2f), new Color(0.9f, 0.4f, 0.9f));
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)UnityEngine.Random.Range(10, 21))
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingOrder = 3;

        particles.Play();
        Destroy(vfxObject, 1f);

        if (doorBounceRoutine != null)
        {
            StopCoroutine(doorBounceRoutine);
        }
        doorBounceRoutine = StartCoroutine(PlayDoorBounce(doorTransform));
    }

    private IEnumerator PlayDoorBounce(Transform target)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 baseScale = target.localScale;
        Vector3 peakScale = baseScale * 1.15f;
        float upDuration = 0.07f;
        float downDuration = 0.09f;
        float elapsed = 0f;

        while (elapsed < upDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / upDuration);
            target.localScale = Vector3.Lerp(baseScale, peakScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < downDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / downDuration);
            target.localScale = Vector3.Lerp(peakScale, baseScale, t);
            yield return null;
        }

        target.localScale = baseScale;
    }

    private void ApplySpriteOrFallback(SpriteRenderer renderer, string folder, string spriteName, Color fallbackColor)
    {
        Sprite sprite = LoadSprite(folder, spriteName);
        if (sprite != null)
        {
            renderer.sprite = sprite;
            renderer.color = Color.white;
            return;
        }

        renderer.sprite = squareSprite;
        renderer.color = fallbackColor;
    }

    private Sprite LoadSprite(string folder, string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return null;
        }

        return Resources.Load<Sprite>(folder + spriteName);
    }

    private void ReloadLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    private void LoadNextLevel()
    {
        if (levelAssets.Count == 0)
        {
            Debug.LogError("No levels available to load.");
            return;
        }

        int nextIndex = Mod(currentLevelIndex + 1, levelAssets.Count);
        LoadLevel(nextIndex);
    }

    private int Mod(int value, int modulus)
    {
        if (modulus <= 0)
        {
            return 0;
        }

        int result = value % modulus;
        return result < 0 ? result + modulus : result;
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

    private float GetHudHeightPixels()
    {
        if (hudRectTransform == null || hudCanvas == null)
        {
            return 0f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(hudRectTransform);
        return Mathf.Max(0f, hudRectTransform.rect.height * hudCanvas.scaleFactor);
    }

    private void UpdateCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        float gridWorldWidth = levelWidth * tileSize;
        float gridWorldHeight = levelHeight * tileSize;
        float aspect = camera.aspect;

        float horizontalPadding = tileSize * 0.5f;
        float bottomPadding = tileSize * 0.5f;
        float topPadding = tileSize * 1.5f;

        float hudHeightPixels = GetHudHeightPixels();
        float availableHeightFraction = 1f;
        if (Screen.height > 0)
        {
            availableHeightFraction = Mathf.Clamp01(1f - (hudHeightPixels / Screen.height));
        }
        availableHeightFraction = Mathf.Max(availableHeightFraction, 0.1f);

        float sizeForHeight = (gridWorldHeight + topPadding + bottomPadding) * 0.5f / availableHeightFraction;
        float sizeForWidth = (gridWorldWidth + horizontalPadding * 2f) * 0.5f / aspect;
        float size = Mathf.Max(sizeForHeight, sizeForWidth);

        float gridLeftEdge = -tileSize * 0.5f;
        float gridTopEdge = tileSize * 0.5f;
        float centerX = gridLeftEdge + gridWorldWidth * 0.5f;
        float hudWorldHeight = (1f - availableHeightFraction) * size * 2f;
        float centerY = gridTopEdge + topPadding - size + hudWorldHeight;

        camera.transform.position = new Vector3(centerX, centerY, -10f);
        camera.orthographicSize = size;
    }
}
