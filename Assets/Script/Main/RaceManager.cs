using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class RaceManager : MonoBehaviour
{
    [Header("Prefabs & Objects")]
    public List<GameObject> pigPrefabs;
    public Transform startPoint;
    public GameObject background; 
    public GameObject winLine; 
    public Transform spawnPoint; 
    public Transform stopPoint; 

    [Header("UI References")]
    public GameObject victoryPanel;
    public Text victoryText;
    public Text countdownText;
    public BoxCollider2D spawnArea;
    
    [Header("Settings")]
    [SerializeField] private float backgroundOffsetSpeed = 0.1f;
    [SerializeField] private float winLineMoveSpeed = 5f;
    [SerializeField] private float winLineSpawnDelay = 10f;
    [SerializeField] private float cameraFollowOffset = 5f;
    [SerializeField] private float cameraLerpSpeed = 2f;
    [SerializeField] private AudioManager audioManager;
    
    [Header("Prefab Canvas")]
    [SerializeField] private GameObject selectionPanelPrefab; 
    [SerializeField] private GameObject inputFieldPrefab;

    private Transform inputFieldContainer;
    private ScrollRect scrollRect;
    
    private List<GameObject> pigs = new List<GameObject>();
    private List<string> playerNames = new List<string>();
    private bool raceStarted = false;
    private bool isOffsetActive = true;
    private bool isCameraLocked = false;
    private GameObject winLineInstance;
    private Camera mainCamera;
    private CanvasGroup selectionPanelInstance;
    private List<InputField> inputFields = new List<InputField>();
    private List<string> enteredNames = new List<string>();
    private int inputFieldCounter = 0;
    private const int MAX_INPUT_FIELDS = 100;
    private const int MAX_NAME_LENGTH = 10;
    private Text errorText;
    private GameObject dimOverlay;
    private int previousScreenWidth;
    private int previousScreenHeight;
    private SpawnPointController spawnPointController;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera không được tìm thấy!");
            return;
        }

        // Tìm SpawnPointController
        spawnPointController = FindObjectOfType<SpawnPointController>();
        if (spawnPointController == null)
        {
            Debug.LogError("SpawnPointController không được tìm thấy!");
            return;
        }

        previousScreenWidth = Screen.width;
        previousScreenHeight = Screen.height;

        // Bắt đầu khởi tạo nhưng trì hoãn SpawnPigs
        StartCoroutine(InitializeRaceWithDelay());
    }

    IEnumerator InitializeRaceWithDelay()
    {
        // Đợi một frame để đảm bảo SpawnPointController cập nhật vị trí
        yield return null;

        InitializeRace();
    }
    
    void InitializeRace()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);

        LoadPlayerData();
        // Gọi SpawnPigs ngay sau khi đảm bảo startPoint được cập nhật
        SpawnPigs();
        StartCoroutine(Countdown());
        if (winLineInstance == null) StartCoroutine(SpawnWinLineAfterDelay());
    }

    void LoadPlayerData()
    {
        int playerCount = PlayerPrefs.GetInt("PlayerCount", 0);
        playerCount = Mathf.Min(playerCount, MAX_INPUT_FIELDS);

        if (playerCount <= 0)
        {
            Debug.LogWarning("Không có người chơi nào được tải!");
            return;
        }

        playerNames.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            playerNames.Add(PlayerPrefs.GetString($"PlayerName_{i}", $"Player {i + 1}"));
        }

        Shuffle(playerNames);
    }

    void SpawnPigs()
    {
        Debug.Log("Bắt đầu SpawnPigs tại " + System.DateTime.Now.ToString("HH:mm:ss") + " ngày 26/6/2025");

        if (pigPrefabs == null || pigPrefabs.Count == 0)
        {
            Debug.LogError("Danh sách pigPrefabs rỗng hoặc null!");
            return;
        }
        if (startPoint == null)
        {
            Debug.LogError("startPoint chưa được gán!");
            return;
        }
        if (spawnArea == null)
        {
            Debug.LogError("spawnArea chưa được gán!");
            return;
        }

        // Đảm bảo startPoint được cập nhật trước khi spawn
        if (spawnPointController != null)
        {
            spawnPointController.UpdateSpawnPointPosition();
        }

        foreach (GameObject pig in pigs)
        {
            if (pig != null) Destroy(pig);
        }
        pigs.Clear();
        Debug.Log("Đã xóa danh sách pigs cũ, kích thước hiện tại: " + pigs.Count);

        int pigsToSpawn = playerNames.Count;
        Debug.Log("Số heo cần spawn: " + pigsToSpawn + ", Số tên người chơi: " + playerNames.Count);
        if (pigsToSpawn == 0)
        {
            Debug.LogWarning("Không có người chơi để spawn lợn!");
            return;
        }

        List<int> availableIndices = Enumerable.Range(0, pigPrefabs.Count).ToList();
        Shuffle(availableIndices);
        Debug.Log("Số prefab có sẵn: " + pigPrefabs.Count + ", Số index random: " + availableIndices.Count);

        List<string> randomizedNames = new List<string>(playerNames);
        Dictionary<string, float> randomValues = new Dictionary<string, float>();
        foreach (string name in randomizedNames)
        {
            randomValues[name] = UnityEngine.Random.value;
        }
        randomizedNames.Sort((a, b) => randomValues[a].CompareTo(randomValues[b]));
        Debug.Log("Danh sách tên random: " + string.Join(", ", randomizedNames));

        Vector2 areaSize = spawnArea.size;
        Vector2 areaCenter = startPoint.position; // Sử dụng startPoint.position thay vì spawnArea.transform.position
        float minY = areaCenter.y - areaSize.y / 2f;
        float maxY = areaCenter.y + areaSize.y / 2f;
        float baseStepY = areaSize.y / (pigsToSpawn > 0 ? pigsToSpawn : 1);
        Debug.Log($"Kích thước spawnArea: {areaSize}, minY: {minY}, maxY: {maxY}, baseStepY: {baseStepY}");

        for (int i = 0; i < pigsToSpawn; i++)
        {
            int prefabIndex = availableIndices[i % availableIndices.Count];
            float yPos = minY + i * baseStepY;
            Vector3 spawnPos = new Vector3(areaCenter.x, yPos, 0);

            if (yPos < minY || yPos > maxY)
            {
                Debug.LogWarning($"Vị trí Y {yPos} vượt quá vùng spawnArea, điều chỉnh lại!");
                yPos = Mathf.Clamp(yPos, minY, maxY);
                spawnPos.y = yPos;
            }

            GameObject pig = Instantiate(pigPrefabs[prefabIndex], spawnPos, Quaternion.identity);
            pig.name = $"Pig_{randomizedNames[i]}";
            Debug.Log($"Spawn heo {pig.name} tại vị trí: {spawnPos}, index spawn: {i}");

            BoxCollider2D pigCollider = pig.GetComponent<BoxCollider2D>();
            if (pigCollider == null)
            {
                Debug.LogWarning("Pig prefab không có BoxCollider2D, thêm mới!");
                pigCollider = pig.AddComponent<BoxCollider2D>();
                pigCollider.size = new Vector2(1f, 1f);
            }

            PigController pigController = pig.GetComponent<PigController>();
            if (pigController == null)
            {
                pigController = pig.AddComponent<PigController>();
            }
            pigController.SetName(randomizedNames[i]);
            pigs.Add(pig);
            Debug.Log($"Đã thêm heo {pig.name} vào danh sách, tổng số: {pigs.Count}");
        }

        ApplyZOrderByYPosition();
    }

    // Phương thức áp dụng z dựa trên tọa độ Y
    private void ApplyZOrderByYPosition()
    {
        Debug.Log("Bắt đầu áp dụng z theo Y tại " + System.DateTime.Now.ToString("HH:mm:ss") + " ngày 26/6/2025");

        if (pigs == null)
        {
            Debug.LogError("Danh sách pigs là null!");
            return;
        }
        Debug.Log("Số heo trong danh sách: " + pigs.Count);

        // Sắp xếp pigs theo tọa độ Y tăng dần (từ thấp đến cao)
        var sortedPigs = pigs.OrderBy(pig => pig.transform.position.y).ToList();

        // Gán z, bắt đầu từ -9.99 cho Y thấp nhất, giảm dần
        float zValue = -9.7f;
        float zStep = 0.01f; // tăng 0.01 cho mỗi heo tiếp theo

        for (int i = 0; i < sortedPigs.Count; i++)
        {
            int rank = i + 1; // Số thứ tự bắt đầu từ 1
            Vector3 position = sortedPigs[i].transform.position;
            position.z = zValue; // Gán z mới
            sortedPigs[i].transform.position = position;
            Debug.Log($"Heo {sortedPigs[i].name} - Thứ tự: {rank}, Y={position.y}, z={position.z}");
            zValue += zStep; // Giảm z cho heo tiếp theo
        }
    }

    void Update()
    {
        UpdateBackground();
        ApplyZOrderByYPosition();
        if (inputFieldContainer != null && (Screen.width != previousScreenWidth || Screen.height != previousScreenHeight))
        {
            OnRectTransformDimensionsChange();
            previousScreenWidth = Screen.width;
            previousScreenHeight = Screen.height;
        }
    }

    void UpdateBackground()
    {
        if (raceStarted && isOffsetActive && background != null)
        {
            Renderer renderer = background.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Material mat = renderer.material;
                Vector2 offset = mat.mainTextureOffset;
                offset.x += backgroundOffsetSpeed * Time.deltaTime;
                mat.mainTextureOffset = offset;
            }
        }
    }

    void LateUpdate()
    {
        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        if (!raceStarted || pigs.Count == 0 || mainCamera == null || isCameraLocked) return;

        float maxX = GetLeadingPigPosition();
        Vector3 camPos = mainCamera.transform.position;
        float targetX = maxX + cameraFollowOffset;
        camPos.x = Mathf.Max(camPos.x, targetX);
        mainCamera.transform.position = camPos;
    }

    float GetLeadingPigPosition()
    {
        float maxX = float.MinValue;
        foreach (GameObject pig in pigs)
        {
            if (pig != null && pig.transform.position.x > maxX)
            {
                maxX = pig.transform.position.x;
            }
        }
        return maxX;
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    IEnumerator Countdown()
    {
        if (countdownText == null)
        {
            Debug.LogError("CountdownText chưa được gán!");
            yield break;
        }

        countdownText.gameObject.SetActive(true);
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "Bắt đầu!";
        yield return new WaitForSeconds(0.5f);
        countdownText.gameObject.SetActive(false);
        StartRace();
    }

    void StartRace()
    {
        raceStarted = true;
        for (int i = 0; i < pigs.Count && i < playerNames.Count; i++)
        {
            PigController pigController = pigs[i].GetComponent<PigController>();
            if (pigController != null)
            {
                pigController.SetName(playerNames[i]);
                pigController.StartInitialMovement();
            }
        }
    }

    IEnumerator SpawnWinLineAfterDelay()
    {
        yield return new WaitForSeconds(winLineSpawnDelay);
        SpawnWinLine();
        foreach (GameObject pig in pigs)
        {
            PigController pigController = pig.GetComponent<PigController>();
            if (pigController != null)
            {
                pigController.StartMoving();
            }
        }
    }

    void SpawnWinLine()
    {
        if (winLine == null || spawnPoint == null)
        {
            Debug.LogError("WinLine prefab hoặc spawnPoint chưa được gán!");
            return;
        }

        if (winLineInstance == null)
        {
            winLineInstance = Instantiate(winLine, spawnPoint.position, Quaternion.identity);
            winLineInstance.SetActive(true);
            winLineInstance.name = "WinLine_Instance";
            SetupWinLine(winLineInstance);
        }
    }

    void SetupWinLine(GameObject winLineObj)
    {
        SpriteRenderer sr = winLineObj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = winLineObj.AddComponent<SpriteRenderer>();
        }
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 10;

        BoxCollider2D collider = winLineObj.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = winLineObj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1f, 10f);
        }

        if (!winLineObj.CompareTag("WinLine"))
        {
            winLineObj.tag = "WinLine";
        }
    }

    public void OnPigWin(string pigName)
    {
        StartCoroutine(OnPigWinCoroutine(pigName));
    }

    private IEnumerator OnPigWinCoroutine(string pigName)
    {
        foreach (GameObject pig in pigs)
        {
            PigController pigController = pig.GetComponent<PigController>();
            if (pigController != null)
            {
                pigController.StopMoving();
            }
        }
        ShowVictory(pigName);
        if (audioManager != null) audioManager.OnWinLineTrigger();
        yield return new WaitForSeconds(2f);
        EndRace();
    }

    void EndRace()
    {
        raceStarted = false;
        isOffsetActive = false;
    }

    void ShowVictory(string winnerName)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        if (victoryText != null)
        {
            victoryText.text = $" {winnerName}";
        }
    }

    public void OnCameraTriggerWinLine()
    {
        if (!raceStarted) return;
        isOffsetActive = false;
        isCameraLocked = true;
        foreach (GameObject pig in pigs)
        {
            PigController pigController = pig.GetComponent<PigController>();
            if (pigController != null)
            {
                pigController.DoubleSpeed();
            }
        }
    }

    public void RestartRace()
    {
        GameObject sceneCanvas = GameObject.Find("Canvas");
        if (sceneCanvas != null)
        {
            sceneCanvas.gameObject.SetActive(false);
        }

        ShowSelectionPanel();
    }

    private void ShowSelectionPanel()
    {
        if (selectionPanelInstance != null || selectionPanelPrefab == null)
        {
            return;
        }

        GameObject parentObj = new GameObject("SelectionPanelParent");
        GameObject panelObj = Instantiate(selectionPanelPrefab, parentObj.transform);
        selectionPanelInstance = panelObj.GetComponent<CanvasGroup>();
        Canvas canvasComponent = panelObj.GetComponent<Canvas>();

        if (canvasComponent != null)
        {
            canvasComponent.enabled = true;
            canvasComponent.sortingOrder = 20;
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.overrideSorting = true;
        }
        else
        {
            Debug.LogError("Không tìm thấy Canvas component trong selectionPanelPrefab");
            return;
        }

        StartCoroutine(FindAndActivateWithTag(panelObj));

        if (selectionPanelInstance != null)
        {
            selectionPanelInstance.alpha = 0f; 
            selectionPanelInstance.blocksRaycasts = false; 
            selectionPanelInstance.interactable = false;
            selectionPanelInstance.gameObject.SetActive(true);
            StartCoroutine(FadeInPanel()); 
        }
        else
        {
            Debug.LogError("Không tìm thấy CanvasGroup trong selectionPanelInstance");
            return;
        }
        GameObject bgObj = panelObj.transform.Find("bg")?.gameObject;
        if (bgObj != null) bgObj.SetActive(false);

        GameObject startBtObj = panelObj.transform.Find("StartBt")?.gameObject;
        if (startBtObj != null) startBtObj.SetActive(false);
    }

    private IEnumerator FindAndActivateWithTag(GameObject panelObj)
    {
        yield return null; 

        dimOverlay = panelObj.GetComponentsInChildren<Transform>(includeInactive: true)
            .Where(t => t.gameObject.CompareTag("DimTag"))
            .Select(t => t.gameObject)
            .FirstOrDefault();
        if (dimOverlay != null)
        {
            dimOverlay.SetActive(true);
            StartCoroutine(FadeInDimOverlay()); 
        }
        else
        {
            Debug.LogError("Không tìm thấy object với tag 'DimTag' trong selectionPanelInstance");
        }

        GameObject selectionPanelObj = panelObj.GetComponentsInChildren<Transform>(includeInactive: true)
            .Where(t => t.gameObject.CompareTag("SelectionPanelTag"))
            .Select(t => t.gameObject)
            .FirstOrDefault();
        Transform selectionPanelTransform = selectionPanelObj != null ? selectionPanelObj.transform : null;
        if (selectionPanelTransform != null)
        {
            selectionPanelTransform.gameObject.SetActive(true);

            Button batDauButton = selectionPanelTransform.Find("BắtĐầuBt")?.GetComponent<Button>();
            Button trangChuButton = selectionPanelTransform.Find("TrangChủBt")?.GetComponent<Button>();
            Button nhapLaiButton = selectionPanelTransform.Find("NhậplạiBt")?.GetComponent<Button>();
            
            // Find ScrollView and InputFieldContainer
            Transform scrollViewTransform = selectionPanelTransform.Find("Scroll View");
            if (scrollViewTransform != null)
            {
                scrollRect = scrollViewTransform.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    Transform viewportTransform = scrollViewTransform.Find("Viewport");
                    if (viewportTransform != null)
                    {
                        inputFieldContainer = viewportTransform.Find("InputFieldContainer");
                        if (inputFieldContainer == null)
                        {
                            // Create InputFieldContainer if it doesn't exist
                            GameObject containerObj = new GameObject("InputFieldContainer");
                            containerObj.transform.SetParent(viewportTransform, false);
                            inputFieldContainer = containerObj.transform;
                            
                            // Set up content for ScrollRect
                            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
                            containerRect.anchorMin = new Vector2(0, 1);
                            containerRect.anchorMax = new Vector2(1, 1);
                            containerRect.pivot = new Vector2(0.5f, 1);
                            containerRect.anchoredPosition = Vector2.zero;
                            
                            // Add content size fitter
                            ContentSizeFitter csf = containerObj.AddComponent<ContentSizeFitter>();
                            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                            
                            // Add vertical layout group
                            VerticalLayoutGroup vlg = containerObj.AddComponent<VerticalLayoutGroup>();
                            vlg.childAlignment = TextAnchor.UpperCenter;
                            vlg.childControlWidth = true;
                            vlg.childControlHeight = false;
                            vlg.childForceExpandWidth = true;
                            vlg.childForceExpandHeight = false;
                            vlg.spacing = 10f;
                            
                            // Set as content for ScrollRect
                            scrollRect.content = containerRect;
                        }
                    }
                }
            }
            
            errorText = selectionPanelTransform.Find("ErrorText")?.GetComponent<Text>();

            if (batDauButton != null)
            {
                batDauButton.onClick.RemoveAllListeners();
                batDauButton.onClick.AddListener(OnBatDauButtonClicked);
            }

            if (trangChuButton != null)
            {
                trangChuButton.onClick.RemoveAllListeners();
                trangChuButton.onClick.AddListener(OnTrangChuButtonClicked);
            }

            if (nhapLaiButton != null)
            {
                nhapLaiButton.onClick.RemoveAllListeners();
                nhapLaiButton.onClick.AddListener(OnNhapLaiButtonClicked);
            }

            // Tạo InputField nếu cần
            if (inputFieldContainer != null && inputFields.Count == 0)
            {
                if (inputFieldPrefab != null)
                {
                    AddNewInputField(inputFieldContainer, inputFieldPrefab, errorText);
                }
                else
                {
                    Debug.LogError("InputFieldPrefab chưa được gán trong Inspector. Vui lòng gán từ Assets/Scenes/Prefabs!");
                }
            }

            if (inputFieldContainer != null)
            {
                PopulateSavedNames(inputFieldContainer);
            }
        }
        else
        {
            Debug.LogError("Không tìm thấy object với tag 'SelectionPanelTag' trong selectionPanelInstance");
        }
    }

   private void PopulateSavedNames(Transform container, bool isFromReset = false)
    {
        if (container == null || inputFieldPrefab == null)
        {
            Debug.LogError("container hoặc inputFieldPrefab là null");
            return;
        }

        // Ensure we have a VerticalLayoutGroup for proper layout
        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 10f;
        }

        // Clear existing fields an toàn hơn
        for (int i = inputFields.Count - 1; i >= 0; i--)
        {
            if (inputFields[i] != null && inputFields[i].gameObject != null)
            {
                Destroy(inputFields[i].gameObject);
            }
        }
        inputFields.Clear();

        // Clear remaining children if any
        foreach (Transform child in container)
        {
            if (child != null && child.gameObject != null)
            {
                Destroy(child.gameObject);
            }
        }

        int requiredFields = Mathf.Max(playerNames.Count > 0 ? playerNames.Count : 1, 1);

        for (int i = 0; i < requiredFields; i++)
        {
            InputField inputField = Instantiate(inputFieldPrefab, container).GetComponent<InputField>();
            
            // Null check sau khi instantiate
            if (inputField == null)
            {
                Debug.LogError($"Failed to create InputField at index {i}");
                continue;
            }
            
            inputField.gameObject.name = $"InputField_{i}";
            
            RectTransform rectTransform = inputField.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                float width = container.GetComponent<RectTransform>().rect.width * 0.9f;
                rectTransform.sizeDelta = new Vector2(width, 50f);
            }

            if (i < playerNames.Count)
            {
                // Hiển thị tên đã lưu và vô hiệu hóa với LockedText
                string text = playerNames[i];
                inputField.text = text;
                inputField.interactable = false;

                // Tạo LockedText
                GameObject newTextObj = new GameObject("LockedText");
                newTextObj.transform.SetParent(inputField.transform, false);
                Text newText = newTextObj.AddComponent<Text>();
                
                // Lấy tham chiếu đến text gốc
                Transform textTransform = inputField.transform.Find("Text (Legacy)");
                Text inputText = null;
                if (textTransform != null)
                {
                    inputText = textTransform.GetComponent<Text>();
                    if (inputText != null)
                    {
                        newText.font = inputText.font;
                        newText.fontSize = inputText.fontSize;
                        newText.fontStyle = inputText.fontStyle;
                        newText.color = inputText.color;
                        inputText.gameObject.SetActive(false);
                    }
                }
                
                newText.alignment = TextAnchor.MiddleCenter;
                newText.text = text;
                
                // Cấu hình RectTransform cho LockedText
                RectTransform newTextRect = newText.GetComponent<RectTransform>();
                newTextRect.anchorMin = new Vector2(0, 0);
                newTextRect.anchorMax = new Vector2(1, 1);
                newTextRect.offsetMin = new Vector2(5, 5);
                newTextRect.offsetMax = new Vector2(-5, -5);

                // Ẩn placeholder
                Transform placeholder = inputField.transform.Find("Placeholder");
                if (placeholder != null)
                {
                    placeholder.gameObject.SetActive(false);
                }

                CanvasGroup canvasGroup = inputField.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = inputField.gameObject.AddComponent<CanvasGroup>();
                }
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                // Không tạo InputField trống, chỉ xử lý các tên đã lưu
                Destroy(inputField.gameObject);
                continue;
            }
            
            inputFields.Add(inputField);
        }

        // Force layout update
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1.0f; // Scroll to top
        }
    }

    private void OnBatDauButtonClicked()
    {
        if (playerNames.Count == 0)
        {
            ShowErrorMessage("Vui lòng nhập ít nhất một tên!", errorText);
            return;
        }

        Debug.Log("Nút Bắt Đầu được nhấn. Lưu " + playerNames.Count + " tên: " + string.Join(", ", playerNames));

        // Lưu số lượng và tên người chơi vào PlayerPrefs
        PlayerPrefs.SetInt("PlayerCount", playerNames.Count);
        for (int i = 0; i < playerNames.Count; i++)
        {
            PlayerPrefs.SetString($"PlayerName_{i}", playerNames[i]);
        }
        PlayerPrefs.Save();

        // Ẩn panel và overlay
        if (selectionPanelInstance != null)
        {
            selectionPanelInstance.gameObject.SetActive(false);
        }
        if (dimOverlay != null)
        {
            dimOverlay.SetActive(false);
        }

        // Load lại scene Main
        SceneManager.LoadScene("Main");
    }

    public void OnTrangChuButtonClicked()
    {
        SceneManager.LoadScene("StartScreen");
    }
    
    private void OnNhapLaiButtonClicked()
    {
        // Clear và destroy InputField an toàn hơn
        for (int i = inputFields.Count - 1; i >= 0; i--)
        {
            if (inputFields[i] != null && inputFields[i].gameObject != null)
            {
                Destroy(inputFields[i].gameObject);
            }
        }
        inputFields.Clear();
        
        playerNames.Clear();
        enteredNames.Clear();
        inputFieldCounter = 0;

        // Tạo một InputField mới
        if (inputFieldContainer != null && inputFieldPrefab != null)
        {
            AddNewInputField(inputFieldContainer, inputFieldPrefab, errorText);
        }
        else
        {
            Debug.LogError("Không thể tạo InputField mới: inputFieldContainer hoặc inputFieldPrefab là null");
        }

        // Force layout update
        if (inputFieldContainer != null)
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1.0f;
            }
        }
    }

    private void AddNewInputField(Transform container, GameObject inputFieldPrefab, Text errorText)
    {
        if (container == null || inputFieldPrefab == null) return;

        // Ensure VerticalLayoutGroup exists
        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 10f;
        }

        // Add ContentSizeFitter if needed
        ContentSizeFitter csf = container.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            csf = container.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        InputField newInputField = Instantiate(inputFieldPrefab, container).GetComponent<InputField>();
        newInputField.gameObject.name = $"InputField_{inputFieldCounter++}";

        // Set up RectTransform
        RectTransform rectTransform = newInputField.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float containerWidth = container.GetComponent<RectTransform>().rect.width;
            float fieldWidth = Mathf.Min(containerWidth * 0.9f, Screen.width * 0.8f);
            float fieldHeight = 50f;
            rectTransform.sizeDelta = new Vector2(fieldWidth, fieldHeight);
            
            // Add layout element to ensure height is respected
            LayoutElement le = newInputField.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = newInputField.gameObject.AddComponent<LayoutElement>();
            }
            le.preferredHeight = fieldHeight;
            le.flexibleWidth = 1;
        }

        // Configure text component
        Transform textTransform = newInputField.transform.Find("Text (Legacy)");
        Text inputText = null;
        if (textTransform != null)
        {
            inputText = textTransform.GetComponent<Text>();
            if (inputText != null)
            {
                inputText.alignment = TextAnchor.MiddleCenter;
                float fontSize = Mathf.Min(Screen.height * 0.3f, 30f);
                inputText.fontSize = Mathf.RoundToInt(fontSize);
            }
        }

        // Configure placeholder
        Transform placeholder = newInputField.transform.Find("Placeholder");
        if (placeholder != null)
        {
            Text placeholderText = placeholder.GetComponent<Text>();
            if (placeholderText != null)
            {
                if (inputFields.Count == 0)
                {
                    placeholderText.text = "NHẬP DANH SÁCH\n(XUỐNG HÀNG THÊM LỰA CHỌN)";
                    placeholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    placeholderText.verticalOverflow = VerticalWrapMode.Overflow;
                    placeholderText.alignment = TextAnchor.MiddleCenter;
                    float fontSize = Mathf.Min(Screen.height * 0.25f, 28f);
                    placeholderText.fontSize = Mathf.RoundToInt(fontSize);
                }
                else
                {
                    placeholder.gameObject.SetActive(false);
                }
            }
        }

        // Set up input handling
        newInputField.onEndEdit.RemoveAllListeners();
        newInputField.onEndEdit.AddListener((text) => OnInputFieldEndEdit(text, newInputField, errorText));
        inputFields.Add(newInputField);
        
        // Focus the field and scroll to it
        StartCoroutine(FocusInputField(newInputField));
        StartCoroutine(ScrollToNewInputField(newInputField));
        
        // Force layout update
        Canvas.ForceUpdateCanvases();
        
    }

    private IEnumerator ScrollToNewInputField(InputField newField)
    {
        yield return null; // Wait one frame
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f; // Scroll to bottom
            scrollRect.velocity = Vector2.zero; // Stop scroll momentum
        }
    }

    private void OnInputFieldEndEdit(string text, InputField currentField, Text errorText)
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // Xử lý chuỗi nhập vào
            text = text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                ShowErrorMessage("Tên không được để trống!", errorText);
                return;
            }
            if (text.Length > MAX_NAME_LENGTH)
            {
                ShowErrorMessage($"Tên quá dài! Giới hạn là {MAX_NAME_LENGTH} ký tự.", errorText);
                return;
            }
            if (playerNames.Contains(text))
            {
                ShowErrorMessage($"Tên '{text}' đã tồn tại! Vui lòng nhập tên khác.", errorText);
                return;
            }

            // Thêm tên vào danh sách
            playerNames.Add(text);
            Debug.Log($"Đã lưu tên: {text}. Tổng số tên: {playerNames.Count}");
            if (currentField == null) return;

            // Lấy kích thước gốc của InputField
            RectTransform rectTransform = currentField.GetComponent<RectTransform>();
            Vector2 originalSize = rectTransform != null ? rectTransform.sizeDelta : new Vector2(720f, 50f);
            
            // Lấy tham chiếu đến thành phần text gốc
            Transform textTransform = currentField.transform.Find("Text (Legacy)");
            Text inputText = null;
            if (textTransform != null)
            {
                inputText = textTransform.GetComponent<Text>();
            }
            
            // Tạo đối tượng LockedText để thay thế
            GameObject newTextObj = new GameObject("LockedText");
            newTextObj.transform.SetParent(currentField.transform, false);
            Text newText = newTextObj.AddComponent<Text>();
            
            // Sao chép thuộc tính từ text gốc
            if (inputText != null)
            {
                newText.font = inputText.font;
                newText.fontSize = inputText.fontSize;
                newText.fontStyle = inputText.fontStyle;
                newText.color = inputText.color;
                inputText.gameObject.SetActive(false);
            }
            else
            {
                // Thuộc tính mặc định nếu không tìm thấy text gốc
                newText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                newText.fontSize = 30;
                newText.color = Color.black;
            }
            
            newText.alignment = TextAnchor.MiddleCenter;
            newText.text = text;
            
            // Cấu hình RectTransform cho LockedText
            RectTransform newTextRect = newText.GetComponent<RectTransform>();
            newTextRect.anchorMin = new Vector2(0, 0);
            newTextRect.anchorMax = new Vector2(1, 1);
            newTextRect.offsetMin = new Vector2(5, 5);
            newTextRect.offsetMax = new Vector2(-5, -5);

            // Ẩn placeholder
            Transform placeholder = currentField.transform.Find("Placeholder");
            if (placeholder != null)
            {
                placeholder.gameObject.SetActive(false);
            }

            // Vô hiệu hóa InputField nhưng giữ hiển thị
            currentField.interactable = false;
            
            CanvasGroup canvasGroup = currentField.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = currentField.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Đảm bảo kích thước không thay đổi
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = originalSize;
            }
            
            // Thêm InputField mới
            AddNewInputField(inputFieldContainer, inputFieldPrefab, errorText);
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (inputFieldContainer != null)
        {
            float containerWidth = inputFieldContainer.GetComponent<RectTransform>().rect.width;

            // Clean up null references trước khi iterate
            inputFields.RemoveAll(field => field == null || field.gameObject == null);

            foreach (InputField field in inputFields)
            {
                if (field != null && field.gameObject != null)
                {
                    RectTransform rectTransform = field.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        float fieldWidth = Mathf.Min(containerWidth * 0.9f, Screen.width * 0.8f);
                        rectTransform.sizeDelta = new Vector2(fieldWidth, rectTransform.sizeDelta.y);
                    }

                    // Adjust font sizes
                    float baseFontSize = 30f;
                    float screenRatio = (float)Screen.width / Screen.height;
                    float fontSizeMultiplier = Mathf.Clamp(screenRatio * 1.5f, 0.8f, 1.5f);
                    int fontSize = Mathf.RoundToInt(baseFontSize * fontSizeMultiplier);
                    fontSize = Mathf.Clamp(fontSize, 24, 35);

                    // Update text component với null check
                    Transform textTransform = field.transform.Find("Text (Legacy)");
                    if (textTransform != null && textTransform.gameObject != null && textTransform.gameObject.activeInHierarchy)
                    {
                        Text inputText = textTransform.GetComponent<Text>();
                        if (inputText != null)
                        {
                            inputText.fontSize = fontSize;
                        }
                    }

                    // Update locked text với null check
                    Transform lockedText = field.transform.Find("LockedText");
                    if (lockedText != null && lockedText.gameObject != null)
                    {
                        Text textComponent = lockedText.GetComponent<Text>();
                        if (textComponent != null)
                        {
                            textComponent.fontSize = fontSize;
                        }
                    }

                    // Update placeholder với null check
                    Transform placeholder = field.transform.Find("Placeholder");
                    if (placeholder != null && placeholder.gameObject != null && placeholder.gameObject.activeInHierarchy)
                    {
                        Text placeholderText = placeholder.GetComponent<Text>();
                        if (placeholderText != null)
                        {
                            float placeholderFontSize = fontSize * 0.9f;
                            placeholderText.fontSize = Mathf.RoundToInt(placeholderFontSize);
                        }
                    }
                }
            }

            // Force layout update
            if (inputFieldContainer.GetComponent<RectTransform>() != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(inputFieldContainer.GetComponent<RectTransform>());
            }
            Canvas.ForceUpdateCanvases();
        }
    }

    private void OnDestroy()
    {
        // Stop all coroutines to prevent accessing destroyed objects
        StopAllCoroutines();
        
        // Clear references
        if (inputFields != null)
        {
            inputFields.Clear();
        }
    } 

    private void ShowErrorMessage(string message, Text errorText)
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = message;
            StartCoroutine(HideErrorMessageAfterDelay(3f, errorText));
        }
    }

    private IEnumerator HideErrorMessageAfterDelay(float delay, Text errorText)
    {
        yield return new WaitForSeconds(delay);
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }

    private IEnumerator FocusInputField(InputField inputField)
    {
        yield return null;
         if (inputField != null && inputField.gameObject != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    private IEnumerator FadeInPanel()
    {
        if (selectionPanelInstance != null)
        {
            selectionPanelInstance.gameObject.SetActive(true);
            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                selectionPanelInstance.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            selectionPanelInstance.alpha = 1f;
            selectionPanelInstance.blocksRaycasts = true;
            selectionPanelInstance.interactable = true;
        }
    }

    private IEnumerator FadeInDimOverlay()
    {
        if (dimOverlay != null)
        {
            Image dimImage = dimOverlay.GetComponent<Image>();
            if (dimImage != null)
            {
                dimOverlay.SetActive(true);
                float duration = 0.3f;
                float elapsed = 0f;
                Color color = dimImage.color;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    color.a = Mathf.Lerp(0f, 0.85f, elapsed / duration);
                    dimImage.color = color;
                    yield return null;
                }
                color.a = 0.85f;
                dimImage.color = color;
            }
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public bool IsRaceStarted() => raceStarted;
    public List<GameObject> GetPigs() => pigs;
    public List<string> GetPlayerNames() => playerNames;
}