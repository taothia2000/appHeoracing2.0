using UnityEngine;

public class SpawnPointController : MonoBehaviour
{
    [SerializeField]
    private Vector2 offset = Vector2.zero; // Offset tùy chỉnh từ góc dưới trái của camera

    private Camera mainCamera;
    private Vector3 initialPosition;
    private bool followCamera = true; // Biến kiểm soát việc theo dõi camera

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera không được tìm thấy!");
            return;
        }

        // Lưu vị trí ban đầu để reset nếu cần
        initialPosition = transform.position;

        // Cập nhật vị trí SpawnPoint lần đầu
        UpdateSpawnPointPosition();
    }

    void Update()
    {
        // Chỉ cập nhật vị trí nếu vẫn đang theo dõi camera
        if (followCamera)
        {
            UpdateSpawnPointPosition();
        }
    }

    public void UpdateSpawnPointPosition()
    {
        if (mainCamera != null && followCamera)
        {
            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            Vector3 cameraBottomLeft = mainCamera.transform.position - new Vector3(cameraWidth / 2f, cameraHeight / 2f, 0f);

            // Áp dụng offset tùy chỉnh từ góc dưới trái, giữ nguyên z
            transform.position = new Vector3(cameraBottomLeft.x + offset.x, cameraBottomLeft.y + offset.y, transform.position.z);

            // Kiểm tra xem object có tag "Starting line" có hiển thị không
            GameObject startingLine = GameObject.FindGameObjectWithTag("Starting line");
            GameObject WinLine = GameObject.FindGameObjectWithTag("WinLine");
            // Kiểm tra cả 2 objects đều active?
            if (WinLine == null && startingLine != null && startingLine.activeInHierarchy)
            {
                followCamera = false; 
            }

            // Hoặc kiểm tra WinLine active thay vì startingLine?
            if (WinLine != null && startingLine != null && WinLine.activeInHierarchy)
            {
                followCamera = false; 
            }
        }
    }

    // Phương thức để lấy vị trí SpawnPoint
    public Vector3 GetSpawnPosition()
    {
        return transform.position;
    }

    // Phương thức để reset về vị trí ban đầu
    public void ResetPosition()
    {
        transform.position = initialPosition;
        followCamera = true; // Cho phép theo dõi camera lại khi reset
    }

    // Phương thức để thiết lập offset từ script khác nếu cần
    public void SetOffset(Vector2 newOffset)
    {
        offset = newOffset;
        UpdateSpawnPointPosition();
    }
}