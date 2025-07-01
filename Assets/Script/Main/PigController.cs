using System.Collections;
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

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        speed = Random.Range(2.5f, 3.5f);
        raceManager = FindObjectOfType<RaceManager>();
        startPosition = transform.position;

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
        rect.sizeDelta = new Vector2(200, 50);
        rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 40;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.white;
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
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
        speed *= 4f;
    }

    IEnumerator UpdateSpeedRandomly()
    {
        while (!canMoveForward)
        {
            speed = Random.Range(3.5f, 4f);
            yield return new WaitForSeconds(0.8f);
        }
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