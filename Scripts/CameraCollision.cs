using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    [Header("Настройки камеры")]
    public Transform player;
    public Vector3 offset = new Vector3(0, 1.5f, -3f);
    public float minDistance = 0.5f;
    public float smoothSpeed = 10f;
    
    [Header("Обнаружение стен")]
    public LayerMask collisionLayers = ~0;
    public float cameraRadius = 0.2f;

    private Vector3 currentPosition;
    private float currentDistance;
    private Camera playerCamera;  // ← Добавили

    void Start()
    {
        currentDistance = Mathf.Abs(offset.z);
        currentPosition = transform.position;
        
        playerCamera = GetComponent<Camera>();  // ← Получаем камеру
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 idealPosition = player.position + player.TransformDirection(offset);
        Vector3 direction = (idealPosition - player.position).normalized;
        float maxDistance = Vector3.Distance(player.position, idealPosition);

        RaycastHit hit;
        if (Physics.SphereCast(player.position, cameraRadius, direction, out hit, maxDistance, collisionLayers))
        {
            currentDistance = Mathf.Clamp(hit.distance, minDistance, maxDistance);
        }
        else
        {
            currentDistance = Mathf.Lerp(currentDistance, maxDistance, Time.deltaTime * smoothSpeed);
        }

        currentPosition = player.position + direction * currentDistance;
        transform.position = currentPosition;

        // Исправленная строка: сохраняем вертикальный поворот камеры
        if (playerCamera != null)
            transform.rotation = player.rotation * Quaternion.Euler(playerCamera.transform.localEulerAngles.x, 0, 0);
        else
            transform.rotation = player.rotation;
    }
}