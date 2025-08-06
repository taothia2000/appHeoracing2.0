using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PigController : MonoBehaviour
{
    private float speed;
    private TextMeshProUGUI nameText;
    private bool isMoving = false;
    private RaceManager raceManager;
    private Vector3 startPosition;
    private bool canMoveForward = false;
    private Coroutine speedUpdateCoroutine;
    private Rigidbody2D rb;
    private float baseSpeed;
    private bool isUnderPlotTwist = false;

    [Header("Font Settings")]
    [SerializeField] private TMP_FontAsset customFont; // Gán font qua Inspector

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        baseSpeed = Random.Range(1f, 4f);
        speed = baseSpeed;
        raceManager = FindObjectOfType<RaceManager>();
        startPosition = transform.position;

        // Tạo và cấu hình Text cho tên heo
        GameObject textObj = new GameObject("NameText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        textObj.transform.localEulerAngles = new Vector3(0, -180, 0);
        nameText = textObj.AddComponent<TextMeshProUGUI>();
        Canvas canvas = textObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 100;
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 350);
        rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Gán font từ Inspector hoặc fallback
        if (customFont != null)
        {
            nameText.font = customFont;
        }
        else
        {
            Debug.LogWarning("Font không được gán trong Inspector! Sử dụng font mặc định Arial.");
            nameText.font = TMP_FontAsset.CreateFontAsset(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")); // Fallback mặc định
        }
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 50;
        nameText.fontSizeMax = 200;
        nameText.alignment = TextAlignmentOptions.Center;

        // Màu sắc ngẫu nhiên
        nameText.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);

        // Thêm Outline (stroke) với màu và độ dày ngẫu nhiên
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
        outline.effectDistance = new Vector3(Random.Range(2f, 5f), -Random.Range(2f, 5f));

        // Bắt đầu coroutine cho các plot twist ngẫu nhiên
        StartCoroutine(RandomPlotTwist());
    }

    // Các phương thức khác giữ nguyên như cũ
   public void SetName(string playerName)
{
    if (nameText != null)
    {
        nameText.text = playerName;

        // Get the RectTransform
        RectTransform rect = nameText.GetComponent<RectTransform>();

        // Adjust sizeDelta based on name length
        int nameLength = playerName.Length;
            if (nameLength <= 5)
            {
                // Short names: default size
                rect.sizeDelta = new Vector2(300, 350);
                nameText.fontSizeMin = 50;
                nameText.fontSizeMax = 140;
            }
            else if (nameLength <= 10)
            {
                // Medium names: slightly larger width
                rect.sizeDelta = new Vector2(500, 550);
                nameText.fontSizeMin = 50;
                nameText.fontSizeMax = 145;
            }
            else
            {
                // Long names: even larger width
                rect.sizeDelta = new Vector2(700, 750);
                nameText.fontSizeMin = 50;
                nameText.fontSizeMax = 150;
            }
    }
}

    public void StartInitialMovement()
    {
        isMoving = true;
        if (speedUpdateCoroutine != null)
            StopCoroutine(speedUpdateCoroutine);
        speedUpdateCoroutine = StartCoroutine(UpdateSpeedRandomly());
    }

    public void StartMoving()
    {
        canMoveForward = true;
        if (speedUpdateCoroutine != null)
            StopCoroutine(speedUpdateCoroutine);
    }

    public void StopMoving()
    {
        isMoving = false;
    }

    public void DoubleSpeed()
    {
        speed = baseSpeed * 2f;
    }

    IEnumerator UpdateSpeedRandomly()
    {
        while (!canMoveForward)
        {
            speed = Random.Range(1f, 4f);
            yield return new WaitForSeconds(0.8f);
        }
    }

    IEnumerator RandomPlotTwist()
    {
        while (true)
        {
            if (isMoving && canMoveForward && !isUnderPlotTwist)
            {
                int position = GetPigPosition();
                int totalPigs = raceManager.GetPigs().Count;

                if (Random.value < 2f)
                {
                    isUnderPlotTwist = true;
                    float originalSpeed = speed;

                    if (position == 1)
                    {
                        speed = baseSpeed * 0.2f;
                        yield return new WaitForSeconds(5f);
                    }
                    else if (position == totalPigs)
                    {
                        speed = baseSpeed * 2f;
                        yield return new WaitForSeconds(2f);
                    }
                    else
                    {
                        float change = Random.value < 0.4f ? 0.8f : 1f;
                        speed = baseSpeed * change;
                        yield return new WaitForSeconds(2f);
                    }

                    speed = baseSpeed;
                    isUnderPlotTwist = false;
                }
            }

            yield return new WaitForSeconds(Random.Range(1f, 4f));
        }
    }

    private int GetPigPosition()
    {
        var pigs = raceManager.GetPigs();
        if (pigs == null || pigs.Count == 0) return 1;

        var sortedPigs = pigs.OrderByDescending(pig => pig.transform.position.x).ToList();
        return sortedPigs.IndexOf(gameObject) + 1;
    }

    void Update()
    {
        if (isMoving)
        {
            if (!canMoveForward)
            {
                float newX = transform.position.x + speed * Time.deltaTime;
                rb.MovePosition(new Vector3(newX, transform.position.y, transform.position.z));
            }
            else
            {
                rb.MovePosition(rb.position + Vector2.right * speed * Time.deltaTime);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("WinLine") && raceManager.IsRaceStarted() && canMoveForward)
        {
            isMoving = false;
            raceManager.OnPigWin(nameText.text);
        }
    }
}