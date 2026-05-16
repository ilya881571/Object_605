using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 100f;
    
    [Header("Vault Settings")]
    public float vaultHeight = 1.2f;        // Максимальная высота препятствия
    public float vaultDistance = 1.5f;       // Дальность проверки перед игроком
    public float vaultUpForce = 5f;          // Сила прыжка вверх
    public float vaultForwardForce = 3f;     // Сила толчка вперёд
    public float vaultCooldown = 0.5f;       // Задержка между перелезаниями

    [Header("Camera Rotation Limits")]
    public float minVerticalAngle = -60f;
    public float maxVerticalAngle = 40f;

    private Rigidbody rb;
    private Camera playerCamera;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting = false;
    private bool canVault = true;
    private bool isGrounded = true;
    private float verticalRotation = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (rb == null)
            Debug.LogError("Rigidbody не найден на объекте " + gameObject.name);
        if (playerCamera == null)
            Debug.LogError("Камера не найдена!");

        // Настройки Rigidbody для FPS
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && canVault && isGrounded)
        {
            TryVault();
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void Update()
    {
        HandleRotation();
        CheckGrounded();
    }

    void HandleMovement()
    {
        if (rb == null || playerCamera == null) return;

        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward = cameraForward.normalized;
        cameraRight = cameraRight.normalized;

        Vector3 movementDirection = (cameraForward * moveInput.y) + (cameraRight * moveInput.x);
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        rb.linearVelocity = new Vector3(
            movementDirection.x * currentSpeed,
            rb.linearVelocity.y,  // Сохраняем вертикальную скорость (гравитация)
            movementDirection.z * currentSpeed
        );
    }

    void HandleRotation()
    {
        if (playerCamera == null) return;

        // Горизонтальный поворот (персонаж)
        transform.Rotate(0, lookInput.x * rotationSpeed * Time.deltaTime, 0);

        // Вертикальный поворот камеры (с ограничением)
        verticalRotation -= lookInput.y * rotationSpeed * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    void CheckGrounded()
    {
        // Проверяем, на земле ли игрок
        float rayLength = 1.1f;  // Чуть больше высоты капсулы
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayLength);
    }

    void TryVault()
    {
        // Проверяем, есть ли препятствие перед игроком
        Vector3 checkStart = transform.position + Vector3.up * 0.5f;
        Vector3 checkDirection = transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(checkStart, checkDirection, out hit, vaultDistance))
        {
            // Проверяем высоту препятствия
            float obstacleHeight = hit.collider.bounds.max.y - transform.position.y;
            
            if (obstacleHeight <= vaultHeight)
            {
                StartCoroutine(VaultOver(obstacleHeight));
            }
            else
            {
                Debug.Log("Слишком высоко, не перелезть");
            }
        }
    }

    IEnumerator VaultOver(float obstacleHeight)
    {
        canVault = false;

        // Применяем импульс вверх и вперёд
        Vector3 vaultForce = Vector3.up * vaultUpForce + transform.forward * vaultForwardForce;
        rb.AddForce(vaultForce, ForceMode.Impulse);

        // Ждём, пока игрок приземлится
        float waitTime = 0f;
        float maxWaitTime = 2f;  // Максимальное время ожидания приземления

        while (!isGrounded && waitTime < maxWaitTime)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        // Небольшая задержка перед следующим перелезанием
        yield return new WaitForSeconds(0.2f);

        // Убеждаемся, что игрок точно на земле
        if (isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }

        canVault = true;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}