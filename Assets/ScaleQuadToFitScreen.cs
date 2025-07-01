using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ScaleQuadToFitScreen : MonoBehaviour
{
    [SerializeField] private Camera mainCam;

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
        ScaleQuadToFitScreenSize();
    }

    void LateUpdate()
    {
        // Làm cho Quad theo camera
        Vector3 cameraPosition = mainCam.transform.position;
        transform.position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
    }

    private void ScaleQuadToFitScreenSize()
    {
        // Tính kích thước viewport dựa trên orthographic size
        float camHeight = 2f * mainCam.orthographicSize; // Chiều cao = 2 * Size = 10
        float camWidth = camHeight * mainCam.aspect;     // Chiều rộng dựa trên aspect ratio

        // Đặt tỷ lệ Quad để vừa với viewport (không phụ thuộc vào texture)
        transform.localScale = new Vector3(camWidth, camHeight, 1f);

        // Đặt vị trí ban đầu khớp với camera
        Vector3 cameraPosition = mainCam.transform.position;
        transform.position = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
    }
}