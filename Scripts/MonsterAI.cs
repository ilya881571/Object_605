using UnityEngine;
using System.Collections;

public class MonsterAI : MonoBehaviour
{
    [Header("Настройки движения")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 4f;
    public float rotationSpeed = 180f;

    [Header("Камера монстра")]
    public MonsterVision monsterVision;

    [Header("Патруль")]
    public float patrolRadius = 10f;
    public float waitAtPointMin = 1f;
    public float waitAtPointMax = 3f;

    private Rigidbody rb;
    private Vector3 startPosition;
    private Vector3 currentPatrolPoint;
    private bool isChasing = false;
    private bool isWaiting = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        if (monsterVision == null)
            monsterVision = GetComponentInChildren<MonsterVision>();

        SetNewPatrolPoint();
    }

    void Update()
    {
        if (monsterVision == null) return;

        if (monsterVision.IsPlayerVisible())
        {
            isChasing = true;
            StopAllCoroutines();
            isWaiting = false;
        }
    }

    void FixedUpdate()
    {
        if (monsterVision == null) return;

        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void ChasePlayer()
    {
        Vector3 targetPosition = monsterVision.GetLastSeenPosition();
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        if (direction.magnitude > 0.5f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );

            float angleToPlayer = Vector3.Angle(transform.forward, direction);
            if (angleToPlayer < 30f)
            {
                Vector3 velocity = transform.forward * chaseSpeed;
                velocity.y = rb.linearVelocity.y;
                rb.linearVelocity = velocity;
            }
        }
    }

    void Patrol()
    {
        if (isWaiting)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 direction = currentPatrolPoint - transform.position;
        direction.y = 0;
        float distance = direction.magnitude;

        if (distance < 1f)
        {
            if (!isWaiting)
                StartCoroutine(WaitAtPoint());
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        float angleToTarget = Vector3.Angle(transform.forward, direction);
        if (angleToTarget < 30f)
        {
            Vector3 velocity = transform.forward * walkSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    void SetNewPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        currentPatrolPoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        float waitTime = Random.Range(waitAtPointMin, waitAtPointMax);
        yield return new WaitForSeconds(waitTime);
        SetNewPatrolPoint();
        isWaiting = false;
    }
}