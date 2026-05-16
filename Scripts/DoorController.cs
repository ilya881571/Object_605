using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class DoorController : MonoBehaviour
{
    [Header("Настройки")]
    public float openAngle = 90f;
    public float openSpeed = 3f;

    private bool isOpen = false;
    private bool isMoving = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    
    private PlayerInput playerInput;
    private InputAction interactAction;
    private bool playerInTrigger = false;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
        }
        else
        {
            Debug.LogError("PlayerInput не найден в сцене!");
        }
    }

    void Update()
    {
        if (playerInTrigger && interactAction != null && interactAction.WasPressedThisFrame())
        {
            ToggleDoor();
        }
    }

    public void ToggleDoor()
    {
        if (isMoving) return;
        isOpen = !isOpen;
        StopAllCoroutines();
        StartCoroutine(RotateDoor(isOpen ? openRotation : closedRotation));
    }

    IEnumerator RotateDoor(Quaternion targetRotation)
    {
        isMoving = true;
        float duration = 1f / openSpeed;
        float elapsed = 0f;
        Quaternion fromRotation = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.rotation = Quaternion.Lerp(fromRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
        isMoving = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = false;
    }
}