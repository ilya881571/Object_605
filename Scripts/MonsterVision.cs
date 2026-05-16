using UnityEngine;

public class MonsterVision : MonoBehaviour
{
    [Header("Настройки зрения")]
    public float viewDistance = 10f;
    public float viewAngle = 60f;
    public LayerMask obstacleLayers;

    [Header("Цель")]
    public Transform player;

    [Header("Визуал (красный луч)")]
    public bool showRedLight = true;
    public float lightWidth = 0.05f;

    private bool playerVisible = false;
    private Vector3 lastSeenPosition;
    private LineRenderer lineRenderer;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (showRedLight)
        {
            SetupLineRenderer();
        }
    }

    void SetupLineRenderer()
    {
        // Удаляем старый если есть
        LineRenderer oldLR = GetComponent<LineRenderer>();
        if (oldLR != null)
            Destroy(oldLR);

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lightWidth;
        lineRenderer.endWidth = lightWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 0.1f, 0.1f, 0.5f);
        lineRenderer.endColor = new Color(1f, 0f, 0f, 0f);
    }

    void Update()
    {
        bool wasVisible = playerVisible;
        playerVisible = CheckVisibility();

        if (showRedLight && lineRenderer != null)
        {
            // Принудительно обновляем позицию каждый кадр
            Vector3 eyePosition = transform.position;
            Vector3 forward = transform.forward;

            lineRenderer.SetPosition(0, eyePosition);

            if (playerVisible && player != null)
            {
                lineRenderer.SetPosition(1, player.position);
                lineRenderer.startColor = Color.red;
                lineRenderer.endColor = new Color(1f, 0f, 0f, 0.8f);
            }
            else
            {
                Vector3 endPoint = eyePosition + forward * viewDistance;
                RaycastHit hit;
                if (Physics.Raycast(eyePosition, forward, out hit, viewDistance, obstacleLayers))
                {
                    endPoint = hit.point;
                }
                lineRenderer.SetPosition(1, endPoint);
                lineRenderer.startColor = new Color(1f, 0.1f, 0.1f, 0.5f);
                lineRenderer.endColor = new Color(1f, 0f, 0f, 0f);
            }
        }

        // Отладка
        if (playerVisible && !wasVisible)
            Debug.Log("Монстр увидел игрока!");
        else if (!playerVisible && wasVisible)
            Debug.Log("Монстр потерял игрока!");

        // Debug-луч в редакторе
        Debug.DrawRay(transform.position, transform.forward * viewDistance, Color.red);
    }

    bool CheckVisibility()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > viewDistance)
            return false;

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > viewAngle / 2f)
            return false;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer.normalized, out hit, viewDistance, obstacleLayers))
        {
            if (hit.transform.CompareTag("Player"))
            {
                lastSeenPosition = player.position;
                return true;
            }
            return false;
        }

        return false;
    }

    public bool IsPlayerVisible()
    {
        return playerVisible;
    }

    public Vector3 GetLastSeenPosition()
    {
        return lastSeenPosition;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward * viewDistance;
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward * viewDistance;
        Gizmos.DrawRay(transform.position, rightBoundary);
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, transform.forward * viewDistance);
    }
}