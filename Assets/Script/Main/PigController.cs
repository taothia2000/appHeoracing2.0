using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PigController : MonoBehaviour
{
    private float speed;
    private Text nameText;
    private bool isMoving = false;
    private RaceManager raceManager;
    private Vector3 startPosition;
    private bool canMoveForward = false;
    private Coroutine speedUpdateCoroutine;
    private Rigidbody2D rb;
    private float baseSpeed; // Lưu tốc độ gốc để khôi phục sau plot twist
    private bool isUnderPlotTwist = false; // Trạng thái đang bị ảnh hưởng bởi plot twist

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
        nameText = textObj.AddComponent<Text>();
        Canvas canvas = textObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 100; // Đặt sortingOrder cao để tên luôn hiển thị trên cùng
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 80); // Tăng kích thước để phù hợp với font lớn hơn
        rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 60; // Tăng kích thước font để tên to hơn
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.white;
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector3(3, -3); // Tăng viền để rõ hơn với font lớn

        // Bắt đầu coroutine cho các plot twist ngẫu nhiên
        StartCoroutine(RandomPlotTwist());
    }

    public void SetName(string playerName)
    {
        if (nameText != null)
        {
            nameText.text = playerName;
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
        isMoving = false; // Dừng Pig
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
            // Chỉ kích hoạt plot twist khi heo đang di chuyển và không bị ảnh hưởng bởi plot twist khác
            if (isMoving && canMoveForward && !isUnderPlotTwist)
            {
                // Lấy vị trí hiện tại của heo so với các heo khác
                int position = GetPigPosition();
                int totalPigs = raceManager.GetPigs().Count;

                // Xác định xác suất plot twist (10% mỗi 2-5 giây)
                if (Random.value < 0.1f)
                {
                    isUnderPlotTwist = true;
                    float originalSpeed = speed;

                    if (position == 1) // Heo dẫn đầu
                    {
                        // Giảm tốc độ 50% trong 3 giây
                        speed = baseSpeed * 0.5f;
                        Debug.Log($"Heo {nameText.text} dẫn đầu bị giảm tốc độ xuống {speed}!");
                        yield return new WaitForSeconds(3f);
                    }
                    else if (position == totalPigs) // Heo cuối cùng
                    {
                        // Tăng tốc gấp đôi trong 2 giây
                        speed = baseSpeed * 2f;
                        Debug.Log($"Heo {nameText.text} cuối bảng tăng tốc lên {speed}!");
                        yield return new WaitForSeconds(2f);
                    }
                    else // Heo ở giữa
                    {
                        // Thay đổi tốc độ ngẫu nhiên (-20% hoặc +20%)
                        float change = Random.value < 0.5f ? 0.8f : 1.2f;
                        speed = baseSpeed * change;
                        Debug.Log($"Heo {nameText.text} ở giữa thay đổi tốc độ thành {speed}!");
                        yield return new WaitForSeconds(2.5f);
                    }

                    // Khôi phục tốc độ gốc
                    speed = baseSpeed;
                    isUnderPlotTwist = false;
                    Debug.Log($"Heo {nameText.text} khôi phục tốc độ về {speed}.");
                }
            }

            // Chờ ngẫu nhiên từ 2 đến 5 giây trước khi kiểm tra plot twist tiếp theo
            yield return new WaitForSeconds(Random.Range(2f, 5f));
        }
    }

    private int GetPigPosition()
    {
        var pigs = raceManager.GetPigs();
        if (pigs == null || pigs.Count == 0) return 1;

        // Sắp xếp heo theo vị trí X (từ lớn đến nhỏ)
        var sortedPigs = pigs.OrderByDescending(pig => pig.transform.position.x).ToList();
        return sortedPigs.IndexOf(gameObject) + 1; // Vị trí bắt đầu từ 1
    }

    void Update()
    {
        if (isMoving)
        {
            if (!canMoveForward)
            {
                float newX = transform.position.x + speed * Time.deltaTime;
                rb.MovePosition(new Vector3(newX, transform.position.y, transform.position.z)); // Sử dụng Rigidbody
            }
            else
            {
                rb.MovePosition(rb.position + Vector2.right * speed * Time.deltaTime); // Sử dụng Rigidbody
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Heo {nameText.text} chạm {other.gameObject.name} với tag: {other.gameObject.tag} tại vị trí: {transform.position}");
        
        // Check both tag and ensure we're not in the initial spawn phase
        if (other.gameObject.CompareTag("WinLine") && raceManager.IsRaceStarted() && canMoveForward)
        {
            isMoving = false;
            raceManager.OnPigWin(nameText.text);
        }
    }
}