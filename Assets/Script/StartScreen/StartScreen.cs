using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    public Button startButton;
    public Button BắtđầuBt;
    public Button TrangChủBt;
    public Button NhậpLạiBt;
    public GameObject dimOverlay;
    public CanvasGroup selectionPanel;
    public Transform inputFieldContainer;
    public InputField inputFieldPrefab;
    public Text errorText; // Text để hiển thị thông báo lỗi
    private const int MAX_NAME_LENGTH = 10; // Giới hạn độ dài tên
    private List<InputField> inputFields = new List<InputField>();
    private List<string> enteredNames = new List<string>(); // Lưu danh sách tên đã nhập
    private int inputFieldCounter = 0;

    void Start()
    {
        if (dimOverlay != null)
        {
            Image dimImage = dimOverlay.GetComponent<Image>();
            if (dimImage != null)
            {
                Color color = dimImage.color;
                color.a = 0;
                dimImage.color = color;
                dimOverlay.SetActive(false);
            }
        }
        if (selectionPanel != null)
        {
            selectionPanel.alpha = 0f;
            selectionPanel.blocksRaycasts = false;
            selectionPanel.interactable = false;
            selectionPanel.gameObject.SetActive(false);
        }

        if (BắtđầuBt != null)
        {
            BắtđầuBt.onClick.AddListener(OnBắtđầuButtonClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            
        }


        if (TrangChủBt != null)
        {
            TrangChủBt.onClick.AddListener(OnTrangChủButtonClicked);
        }

        if (NhậpLạiBt != null)
        {
            NhậpLạiBt.onClick.AddListener(OnNhậpLạiButtonClicked);
        }

        if (inputFieldContainer != null)
        {
            Canvas canvas = inputFieldContainer.GetComponentInParent<Canvas>();
            if (canvas == null && selectionPanel != null)
            {
                Canvas parentCanvas = selectionPanel.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    inputFieldContainer.SetParent(parentCanvas.transform, false);
                }
            }
        }

        if (errorText != null)
        {
            errorText.text = ""; // Ẩn thông báo lỗi ban đầu
            errorText.gameObject.SetActive(false);
        }
    }

    private void OnBắtđầuButtonClicked()
    {
    if (enteredNames.Count == 0)
    {
        ShowErrorMessage("Vui lòng nhập ít nhất một tên!");
        return;
    }

    PlayerPrefs.SetInt("PlayerCount", enteredNames.Count);
    for (int i = 0; i < enteredNames.Count; i++)
    {
        PlayerPrefs.SetString($"PlayerName_{i}", enteredNames[i]);
    }
    PlayerPrefs.Save(); // Lưu PlayerPrefs

    SceneManager.LoadScene("Main");
    }

    void OnStartButtonClicked()
    {
        startButton.gameObject.SetActive(false);
        StartCoroutine(FadeInDimOverlay());
        StartCoroutine(FadeInPanel());

        if (inputFieldContainer != null && inputFieldPrefab != null && inputFields.Count == 0)
        {
            AddNewInputField();
        }
    }

    void OnTrangChủButtonClicked()
    {
        SceneManager.LoadScene("StartScreen");
    }

    void OnNhậpLạiButtonClicked()
    {
        // Xóa tất cả InputField
        foreach (InputField inputField in inputFields)
        {
            if (inputField != null)
            {
                Destroy(inputField.gameObject);
            }
        }
        inputFields.Clear();
        enteredNames.Clear();
        inputFieldCounter = 0;

        // Tạo lại InputField_0
        if (inputFieldContainer != null && inputFieldPrefab != null)
        {
            AddNewInputField();
        }
    }

  void AddNewInputField()
{
    if (inputFieldContainer == null || inputFieldPrefab == null ) return;

    InputField newInputField = Instantiate(inputFieldPrefab, inputFieldContainer);
    newInputField.gameObject.name = $"InputField_{inputFieldCounter++}";

    // Mobile Input Configuration
    #if UNITY_IOS || UNITY_ANDROID
    // Set keyboard settings for mobile
    newInputField.keyboardType = TouchScreenKeyboardType.Default;
    newInputField.shouldHideMobileInput = false; // Important: don't use additional input field above keyboard
    #endif

    RectTransform rectTransform = newInputField.GetComponent<RectTransform>();
    if (rectTransform != null)
    {
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Neo vào trung tâm
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // Đặt vị trí dựa trên InputField trước đó
        if (inputFields.Count > 0)
        {
            RectTransform lastRect = inputFields[inputFields.Count - 1].GetComponent<RectTransform>();
            if (lastRect != null)
            {
                float newY = lastRect.anchoredPosition.y - lastRect.sizeDelta.y - 10; // Cách 10 đơn vị
                rectTransform.anchoredPosition = new Vector2(0, newY);
            }
        }
        else
        {
            rectTransform.anchoredPosition = new Vector2(0, 0); // Vị trí đầu tiên
        }

        // Giữ kích thước từ prefab
        Vector2 prefabSize = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y);
        rectTransform.sizeDelta = prefabSize; // Áp dụng kích thước gốc
    }

    // FIX: Lưu kích thước gốc để khôi phục sau này
    Vector2 originalSize = rectTransform.sizeDelta;

    Transform textTransform = newInputField.transform.Find("Text (Legacy)");
    Text inputText = null;
    if (textTransform != null)
    {
        inputText = textTransform.GetComponent<Text>();
        if (inputText != null)
        {
            inputText.alignment = TextAnchor.MiddleCenter;
            RectTransform textRect = inputText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.offsetMin = new Vector2(5, 5);
                textRect.offsetMax = new Vector2(-5, -5);
            }
        }
    }

    Transform placeholder = newInputField.transform.Find("Placeholder");
    if (placeholder != null)
    {
        Text placeholderText = placeholder.GetComponent<Text>();
        if (placeholderText != null)
        {
            if (inputFieldCounter == 1)
            {
                placeholderText.text = "NHẬP DANH SÁCH\n(XUỐNG HÀNG THÊM LỰA CHỌN)";
                placeholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
                placeholderText.verticalOverflow = VerticalWrapMode.Overflow;
                placeholderText.alignment = TextAnchor.MiddleCenter;
                RectTransform placeholderRect = placeholderText.GetComponent<RectTransform>();
                if (placeholderRect != null)
                {
                    placeholderRect.anchorMin = new Vector2(0f, 0f);
                    placeholderRect.anchorMax = new Vector2(1f, 1f);
                    placeholderRect.offsetMin = new Vector2(5, 5);
                    placeholderRect.offsetMax = new Vector2(-5, -5);
                }
            }
            else
            {
                placeholder.gameObject.SetActive(false);
            }
        }
    }

    // Handle mobile return key
    #if UNITY_IOS || UNITY_ANDROID
    newInputField.onEndEdit.AddListener((text) => {
        if (TouchScreenKeyboard.visible == false && !string.IsNullOrEmpty(text))
        {
            OnInputFieldEndEdit(text, newInputField, originalSize, inputText);
        }
    });
    #else
    newInputField.onEndEdit.AddListener((text) => OnInputFieldEndEdit(text, newInputField, originalSize, inputText));
    #endif

    inputFields.Add(newInputField);
    StartCoroutine(FocusInputField(newInputField));
    StartCoroutine(ScrollToNewInputField(newInputField)); 
}

    IEnumerator FocusInputField(InputField inputField)
    {
        yield return null;
        
        // Ensure screen is settled before activating keyboard
        yield return new WaitForSeconds(0.1f);
        
        if (inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    void ShowErrorMessage(string message)
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = message;
            StartCoroutine(HideErrorMessageAfterDelay(3f)); // Ẩn sau 3 giây
        }
    }

    IEnumerator HideErrorMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }

    // FIX: Sửa method này để khôi phục đúng kích thước
    void OnInputFieldEndEdit(string text, InputField currentField, Vector2 originalSize, Text inputText)
    {
        // Check for empty text or null field
        if (currentField == null || string.IsNullOrEmpty(text.Trim()))
        {
            return;
        }

        bool isReturnKeyPressed = Input.GetKeyDown(KeyCode.Return);
        bool isMobileKeyboardClosed = false;
        
        #if UNITY_IOS || UNITY_ANDROID
        // On mobile, consider keyboard closing as confirmation
        isMobileKeyboardClosed = !TouchScreenKeyboard.visible && currentField.touchScreenKeyboard != null;
        #endif
        
        if (isReturnKeyPressed || isMobileKeyboardClosed)
        {
            if (currentField != null)
            {
                text = text.Trim();

                // Kiểm tra tên hợp lệ
                if (string.IsNullOrEmpty(text))
                {
                    ShowErrorMessage("Tên không được để trống!");
                    return;
                }

                if (text.Length > MAX_NAME_LENGTH)
                {
                    ShowErrorMessage($"Tên quá dài! Giới hạn là {MAX_NAME_LENGTH} ký tự.");
                    int index = inputFields.IndexOf(currentField);
                    if (index >= 0)
                    {
                        inputFields.RemoveAt(index);
                        Destroy(currentField.gameObject);
                        AddNewInputField(); // Tạo lại InputField mới
                    }
                    return;
                }

                if (enteredNames.Contains(text))
                {
                    ShowErrorMessage($"Tên '{text}' đã tồn tại! Vui lòng nhập tên khác.");
                    return;
                }

                // Lưu text trước khi vô hiệu hóa Input Field
                enteredNames.Add(text);

                // THAY ĐỔI QUAN TRỌNG: Tạo text mới thay vì sử dụng text của InputField
                GameObject newTextObj = new GameObject("LockedText");
                newTextObj.transform.SetParent(currentField.transform, false);
                Text newText = newTextObj.AddComponent<Text>();
                newText.font = inputText.font;
                newText.fontSize = inputText.fontSize;
                newText.fontStyle = inputText.fontStyle;
                newText.color = inputText.color;
                newText.alignment = TextAnchor.MiddleCenter;
                newText.text = text;

                // Setup RectTransform cho text mới
                RectTransform newTextRect = newText.GetComponent<RectTransform>();
                newTextRect.anchorMin = new Vector2(0, 0);
                newTextRect.anchorMax = new Vector2(1, 1);
                newTextRect.offsetMin = new Vector2(5, 5);
                newTextRect.offsetMax = new Vector2(-5, -5);

                // Ẩn text và placeholder gốc
                inputText.gameObject.SetActive(false);
                Transform placeholder = currentField.transform.Find("Placeholder");
                if (placeholder != null)
                {
                    placeholder.gameObject.SetActive(false);
                }

                // Vô hiệu hóa Input Field nhưng giữ nguyên nội dung
                currentField.text = text;
                currentField.interactable = false;

                CanvasGroup canvasGroup = currentField.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = currentField.gameObject.AddComponent<CanvasGroup>();
                }
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                // Đảm bảo kích thước không thay đổi
                RectTransform rectTransform = currentField.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = originalSize;
                }
                rectTransform.SetSiblingIndex(inputFieldContainer.childCount - 1);


                ContentSizeFitter csf = currentField.GetComponent<ContentSizeFitter>();
                if (csf != null) csf.enabled = false;

                LayoutElement le = currentField.GetComponent<LayoutElement>();
                if (le != null) le.ignoreLayout = true;

                Debug.Log("Đã lưu tên: " + text);
                AddNewInputField();
            }
        }
    }
    
    IEnumerator ScrollToNewInputField(InputField newField)
    {
        yield return null; // Chờ 1 frame
        ScrollRect scrollRect = inputFieldContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f; // Cuộn xuống dưới cùng
            scrollRect.velocity = Vector2.zero; // Dừng quán tính cuộn
        }
    }

    IEnumerator FadeInPanel()
    {
        if (selectionPanel != null)
        {
            selectionPanel.gameObject.SetActive(true);
            float duration = 1.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                selectionPanel.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            selectionPanel.alpha = 1f;
            selectionPanel.blocksRaycasts = true;
            selectionPanel.interactable = true;
        }
    }
    

    IEnumerator FadeInDimOverlay()
    {
        if (dimOverlay != null)
        {
            Image dimImage = dimOverlay.GetComponent<Image>();
            if (dimImage != null)
            {
                dimOverlay.SetActive(true);
                float duration = 0.5f;
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
}