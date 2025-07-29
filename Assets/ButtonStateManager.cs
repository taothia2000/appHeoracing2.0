using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonStateManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button bt10s;
    public Button bt20s;
    public Button bt30s;
    
    [System.Serializable]
    public class ButtonSprites
    {
        public Sprite normalSprite;
        public Sprite selectedSprite;
    }
    
    public ButtonSprites bt10sSprites;
    public ButtonSprites bt20sSprites;
    public ButtonSprites bt30sSprites;
    
    private Button currentSelectedButton;
    private string selectedButtonName = "bt10s"; // Mặc định chọn bt10s
    
    void Start()
    {
        // Thiết lập sự kiện click cho các button
        if (bt10s != null)
        {
            bt10s.onClick.AddListener(() => OnButtonClicked(bt10s, "bt10s"));
        }
        
        if (bt20s != null)
        {
            bt20s.onClick.AddListener(() => OnButtonClicked(bt20s, "bt20s"));
        }
        
        if (bt30s != null)
        {
            bt30s.onClick.AddListener(() => OnButtonClicked(bt30s, "bt30s"));
        }
        
        // Tải trạng thái đã lưu
        LoadSelectedState();
    }
    
    void OnButtonClicked(Button clickedButton, string buttonName)
    {
        // Bỏ chọn button hiện tại - đặt về trạng thái normal với sprite riêng của nó
        if (currentSelectedButton != null)
        {
            SetButtonToNormal(currentSelectedButton);
        }
        
        // Chọn button mới - đặt sang trạng thái selected với sprite riêng của nó
        currentSelectedButton = clickedButton;
        selectedButtonName = buttonName;
        SetButtonToSelected(currentSelectedButton, buttonName);
        
        // Lưu trạng thái
        SaveSelectedState();
        
        Debug.Log($"Selected button: {selectedButtonName}");
    }
    
    void SetButtonToNormal(Button button)
    {
        if (button == null) return;
        
        ButtonSprites sprites = GetButtonSprites(button);
        if (sprites != null && sprites.normalSprite != null)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = sprites.normalSprite;
            }
        }
    }
    
    void SetButtonToSelected(Button button, string buttonName)
    {
        if (button == null) return;
        
        ButtonSprites sprites = GetButtonSpritesByName(buttonName);
        if (sprites != null && sprites.selectedSprite != null)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = sprites.selectedSprite;
            }
        }
    }
    
    ButtonSprites GetButtonSprites(Button button)
    {
        if (button == bt10s) return bt10sSprites;
        if (button == bt20s) return bt20sSprites;
        if (button == bt30s) return bt30sSprites;
        return null;
    }
    
    ButtonSprites GetButtonSpritesByName(string buttonName)
    {
        switch (buttonName)
        {
            case "bt10s": return bt10sSprites;
            case "bt20s": return bt20sSprites;
            case "bt30s": return bt30sSprites;
            default: return null;
        }
    }
    
    void SaveSelectedState()
{
    PlayerPrefs.SetString("SelectedButtonForGame", selectedButtonName); 
    PlayerPrefs.Save();
}

    void LoadSelectedState()
    {
        selectedButtonName = PlayerPrefs.GetString("SelectedButtonForGame", "bt10s"); // Thay "SelectedButton" bằng "SelectedButtonForGame"
        Button targetButton = null;
        switch (selectedButtonName)
        {
            case "bt10s":
                targetButton = bt10s;
                break;
            case "bt20s":
                targetButton = bt20s;
                break;
            case "bt30s":
                targetButton = bt30s;
                break;
        }
        if (targetButton != null)
        {
            currentSelectedButton = targetButton;
            SetButtonToSelected(currentSelectedButton, selectedButtonName);
            Button[] allButtons = { bt10s, bt20s, bt30s };
            foreach (Button btn in allButtons)
            {
                if (btn != null && btn != currentSelectedButton)
                {
                    SetButtonToNormal(btn);
                }
            }
        }
    }
    
    // Method để lấy tên button đã chọn từ script khác
    public string GetSelectedButtonName()
    {
        return selectedButtonName;
    }
    
    // Method để thiết lập button được chọn từ script khác
    public void SetSelectedButton(string buttonName)
    {
        Button targetButton = null;
        switch (buttonName)
        {
            case "bt10s":
                targetButton = bt10s;
                break;
            case "bt20s":
                targetButton = bt20s;
                break;
            case "bt30s":
                targetButton = bt30s;
                break;
        }
        
        if (targetButton != null)
        {
            OnButtonClicked(targetButton, buttonName);
        }
    }
}