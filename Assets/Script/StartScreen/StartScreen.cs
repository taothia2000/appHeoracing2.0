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
    public Text errorText;
    private const int MAX_NAME_LENGTH = 10;
    private const int MAX_NAMES = 100;
    private List<InputField> inputFields = new List<InputField>();
    private List<string> enteredNames = new List<string>();
    private int inputFieldCounter = 0;
    private InputField currentInputField; // InputField ban đầu để nhập danh sách

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
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }

        // Tạo InputField ban đầu từ prefab
        CreateInitialInputField();
    }

    void CreateInitialInputField()
    {
        if (inputFieldContainer == null || inputFieldPrefab == null) return;

        currentInputField = Instantiate(inputFieldPrefab, inputFieldContainer);
        currentInputField.gameObject.name = "MainInputField";
        currentInputField.lineType = InputField.LineType.MultiLineNewline;
        currentInputField.onEndEdit.AddListener(OnMainInputFieldEndEdit);

        #if UNITY_IOS || UNITY_ANDROID
        currentInputField.keyboardType = TouchScreenKeyboardType.Default;
        currentInputField.shouldHideMobileInput = false;
        #endif

        RectTransform rectTransform = currentInputField.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, 0);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 100); // Tăng chiều cao cho MultiLine
        }

        Transform placeholder = currentInputField.transform.Find("Placeholder");
        if (placeholder != null)
        {
            Text placeholderText = placeholder.GetComponent<Text>();
            if (placeholderText != null)
            {
                placeholderText.text = "NHẬP DANH SÁCH\n(XUỐNG HÀNG THÊM LỰA CHỌN)";
                placeholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
                placeholderText.verticalOverflow = VerticalWrapMode.Overflow;
                placeholderText.alignment = TextAnchor.MiddleCenter;
            }
        }

        inputFields.Add(currentInputField);
        StartCoroutine(FocusInputField(currentInputField));
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
        PlayerPrefs.Save();

        SceneManager.LoadScene("Main");
    }

    void OnStartButtonClicked()
    {
        startButton.gameObject.SetActive(false);
        StartCoroutine(FadeInDimOverlay());
        StartCoroutine(FadeInPanel());

        if (currentInputField != null)
        {
            currentInputField.gameObject.SetActive(true);
            StartCoroutine(FocusInputField(currentInputField));
        }
    }

    void OnTrangChủButtonClicked()
    {
        SceneManager.LoadScene("StartScreen");
    }

    void OnNhậpLạiButtonClicked()
    {
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

        CreateInitialInputField();
    }

    void OnMainInputFieldEndEdit(string text)
    {
        #if UNITY_IOS || UNITY_ANDROID
        if (!TouchScreenKeyboard.visible && !string.IsNullOrEmpty(text))
        {
            ProcessNameList(text);
        }
        #else
        if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(text))
        {
            ProcessNameList(text);
        }
        #endif
    }

    void ProcessNameList(string inputText)
    {
        if (string.IsNullOrEmpty(inputText.Trim()))
        {
            ShowErrorMessage("Danh sách tên không được để trống!");
            return;
        }

        string[] names = inputText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> validNames = new List<string>();

        if (names.Length > MAX_NAMES)
        {
            ShowErrorMessage($"Tối đa chỉ được {MAX_NAMES} tên!");
            return;
        }

        foreach (string name in names)
        {
            string trimmedName = name.Trim();
            if (string.IsNullOrEmpty(trimmedName))
            {
                continue; // Bỏ qua dòng rỗng
            }
            if (trimmedName.Length > MAX_NAME_LENGTH)
            {
                ShowErrorMessage($"Tên '{trimmedName}' quá dài! Giới hạn là {MAX_NAME_LENGTH} ký tự.");
                return;
            }
            if (enteredNames.Contains(trimmedName) || validNames.Contains(trimmedName))
            {
                ShowErrorMessage($"Tên '{trimmedName}' đã tồn tại!");
                return;
            }
            validNames.Add(trimmedName);
        }

        if (validNames.Count == 0)
        {
            ShowErrorMessage("Không có tên hợp lệ trong danh sách!");
            return;
        }

        // Thêm tên hợp lệ vào enteredNames
        enteredNames.AddRange(validNames);

        // Xóa tất cả InputField cũ (bao gồm currentInputField)
        foreach (InputField inputField in inputFields)
        {
            if (inputField != null)
            {
                Destroy(inputField.gameObject);
            }
        }
        inputFields.Clear();
        inputFieldCounter = 0;

        // Tạo InputField mới cho mỗi tên
        for (int i = 0; i < validNames.Count; i++)
        {
            AddNewInputField(validNames[i]);
        }

        Canvas.ForceUpdateCanvases();
        StartCoroutine(ScrollToTop());
    }

    void AddNewInputField(string name = "")
    {
        if (inputFieldContainer == null || inputFieldPrefab == null) return;

        InputField newInputField = Instantiate(inputFieldPrefab, inputFieldContainer);
        newInputField.gameObject.name = $"InputField_{inputFieldCounter++}";

        #if UNITY_IOS || UNITY_ANDROID
        newInputField.keyboardType = TouchScreenKeyboardType.Default;
        newInputField.shouldHideMobileInput = false;
        #endif

        RectTransform rectTransform = newInputField.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            if (inputFields.Count > 0)
            {
                RectTransform lastRect = inputFields[inputFields.Count - 1].GetComponent<RectTransform>();
                if (lastRect != null)
                {
                    float newY = lastRect.anchoredPosition.y - lastRect.sizeDelta.y - 10;
                    rectTransform.anchoredPosition = new Vector2(0, newY);
                }
            }
            else
            {
                rectTransform.anchoredPosition = new Vector2(0, 0);
            }

            Vector2 prefabSize = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y);
            rectTransform.sizeDelta = prefabSize;
        }

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
            placeholder.gameObject.SetActive(false); // Ẩn placeholder cho InputField hiển thị tên
        }

        // Nếu có tên, hiển thị và khóa InputField
        if (!string.IsNullOrEmpty(name))
        {
            newInputField.text = name;
            newInputField.readOnly = true;

            GameObject newTextObj = new GameObject("LockedText");
            newTextObj.transform.SetParent(newInputField.transform, false);
            Text newText = newTextObj.AddComponent<Text>();
            if (inputText != null)
            {
                newText.font = inputText.font;
                newText.fontSize = inputText.fontSize;
                newText.fontStyle = inputText.fontStyle;
                newText.color = inputText.color;
            }
            else
            {
                newText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                newText.fontSize = 30;
                newText.color = Color.black;
            }
            newText.alignment = TextAnchor.MiddleCenter;
            newText.text = name;

            RectTransform newTextRect = newText.GetComponent<RectTransform>();
            newTextRect.anchorMin = new Vector2(0, 0);
            newTextRect.anchorMax = new Vector2(1, 1);
            newTextRect.offsetMin = new Vector2(5, 5);
            newTextRect.offsetMax = new Vector2(-5, -5);

            if (inputText != null)
            {
                inputText.gameObject.SetActive(false);
            }
        }

        inputFields.Add(newInputField);
    }

    IEnumerator FocusInputField(InputField inputField)
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);
        if (inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }

    IEnumerator ScrollToTop()
    {
        yield return null;
        ScrollRect scrollRect = inputFieldContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f; // Cuộn lên đầu
            scrollRect.velocity = Vector2.zero;
        }
    }

    void ShowErrorMessage(string message)
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = message;
            StartCoroutine(HideErrorMessageAfterDelay(3f));
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