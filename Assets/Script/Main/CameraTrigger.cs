using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraTrigger : MonoBehaviour 
{
    [Header("Settings")]
    [SerializeField] private string targetTag = "WinLine";
    
    private RaceManager raceManager;
    private BoxCollider2D boxCollider;
    private Camera mainCam;
    
    private void Start()
    {
        // Lấy tham chiếu tới RaceManager
        raceManager = FindObjectOfType<RaceManager>();
        if (raceManager == null)
        {
            Debug.LogError("RaceManager không được tìm thấy trong scene!", this);
        }
        
        // Lấy BoxCollider2D và Camera component
        boxCollider = GetComponent<BoxCollider2D>();
        mainCam = GetComponent<Camera>();
        
        if (boxCollider == null)
        {
            Debug.LogError("BoxCollider2D không được tìm thấy trên camera!", this);
        }
        
        if (mainCam == null)
        {
            Debug.LogError("Camera component không được tìm thấy trên GameObject này!", this);
        }
        
        // Cập nhật kích thước BoxCollider2D ngay từ đầu
        UpdateCollider();
    }
    
    private void LateUpdate()
    {
        // Cập nhật kích thước BoxCollider2D để khớp với khung hình camera
        UpdateCollider();
    }
    
    private void UpdateCollider()
    {
        if (boxCollider != null && mainCam != null)
        {
            // Tính kích thước viewport dựa trên orthographic size
            float camHeight = 2f * mainCam.orthographicSize;
            float camWidth = camHeight * mainCam.aspect;

            // Đặt kích thước BoxCollider2D để khớp với khung hình camera
            boxCollider.size = new Vector2(camWidth, camHeight);
            boxCollider.offset = Vector2.zero;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other) 
    { 
        // Kiểm tra tag và loại collider
        if (other.CompareTag(targetTag) && other is EdgeCollider2D) 
        { 
            // Kiểm tra null trước khi gọi method
            if (raceManager != null) 
            { 
                raceManager.OnCameraTriggerWinLine(); 
                Debug.Log("Camera triggered WinLine with EdgeCollider2D!");
            }
            else
            {
                Debug.LogError("RaceManager reference is null!");
            }
        } 
    }
    
    public void SetRaceManager(RaceManager manager)
    {
        raceManager = manager;
    }
}