using UnityEngine;

public class PlayerOnlyCollider : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Тег, которому разрешён проход")]
    public string allowedTag = "Player";

    private Collider[] allColliders;  // Все коллайдеры на объекте

    void Start()
    {
        // Получаем ВСЕ коллайдеры на этом объекте
        allColliders = GetComponents<Collider>();

        if (allColliders.Length == 0)
        {
            Debug.LogError("PlayerOnlyCollider: коллайдеры не найдены на " + gameObject.name);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(allowedTag))
        {
            // Отключаем коллизию игрока со ВСЕМИ коллайдерами стены
            foreach (Collider col in allColliders)
            {
                Physics.IgnoreCollision(col, other, true);
            }
            Debug.Log("Игрок вошёл — все коллайдеры стены пропускают");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(allowedTag))
        {
            // Восстанавливаем коллизию со ВСЕМИ коллайдерами
            foreach (Collider col in allColliders)
            {
                Physics.IgnoreCollision(col, other, false);
            }
            Debug.Log("Игрок вышел — стена снова твёрдая");
        }
    }
}