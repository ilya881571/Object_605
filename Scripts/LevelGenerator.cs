using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    [Header("Префабы комнат")]
    public GameObject roomStart;
    public GameObject roomMonsterLair;
    public GameObject corridorLong;
    public GameObject corridorVeryLong;
    public GameObject corridorVeryLong2;
    public GameObject hallMain;
    public GameObject roomExit;
    public GameObject roomSecret;
    public GameObject miniCorridor;
    public GameObject miniTupik;

    [Header("Настройки генерации")]
    public int corridorsFromHall = 3;        // Количество ответвлений от зала
    public int corridorLengthMin = 1;
    public int corridorLengthMax = 3;
    public int monsterLairMaxCount = 2;
    public int secretRoomChance = 15;       // Шанс секретной комнаты в конце ответвления
    public float overlapCheckRadius = 4f;    // Радиус проверки наложения

    [Header("Отладка")]
    public bool generateOnStart = true;

    private List<GameObject> spawnedRooms = new List<GameObject>();
    private List<Transform> pendingExits = new List<Transform>();
    private int monsterLairCount = 0;
    private int roomCounter = 0;
    private Transform generatorRoot;

    void Start()
    {
        if (generateOnStart) GenerateLevel();
    }

    public void GenerateLevel()
    {
        Debug.Log("=== ГЕНЕРАЦИЯ УРОВНЯ ===");
        generatorRoot = transform;   // <-- исправлено: теперь ссылка на корень существует до очистки
        ClearLevel();

        if (!ValidatePrefabs()) return;

        // 1. Стартовая комната
        GameObject startRoom = SpawnRoom(roomStart, Vector3.zero, Quaternion.identity);
        if (startRoom == null) return;
        startRoom.name = "Room_Start";
        CollectExits(startRoom);
        Debug.Log($"Старт. Выходов: {pendingExits.Count}");

        // Выбираем первый попавшийся выход для соединения с коридором (желательно North или South)
        Transform exitToCorridor = GetExitByType(startRoom, "North") ?? GetExitByType(startRoom, "South") ?? pendingExits[0];
        pendingExits.Remove(exitToCorridor);

        // 2. Коридор от старта к залу (используем miniCorridor)
        GameObject connector = SpawnRoomAtExit(miniCorridor, exitToCorridor);
        if (connector == null) { Debug.LogError("Не удалось пристыковать коридор к старту!"); return; }
        spawnedRooms.Add(connector);
        CollectExits(connector);

        // Находим выход коридора, противоположный тому, которым присоединились (чтобы продолжить путь)
        Transform exitToHall = GetOppositeExit(connector, exitToCorridor);
        if (exitToHall == null) exitToHall = pendingExits[0]; // Запасной вариант
        pendingExits.Remove(exitToHall);

        // 3. Центральный зал
        GameObject hall = SpawnRoomAtExit(hallMain, exitToHall);
        if (hall == null) { Debug.LogError("Не удалось создать зал!"); return; }
        hall.name = "Hall_Center";
        spawnedRooms.Add(hall);
        CollectExits(hall);

        // Удаляем из pendingExits тот выход зала, который использован для соединения с коридором (противоположный exitToHall)
        // Это делается внутри SpawnRoomAtExit, но подчистим на всякий случай
        pendingExits.RemoveAll(e => e == null);

        List<Transform> hallExits = new List<Transform>(pendingExits);
        pendingExits.Clear();

        // 4. Строим ответвления от оставшихся выходов зала
        int builtBranches = 0;
        foreach (Transform exit in hallExits)
        {
            if (builtBranches >= corridorsFromHall)
            {
                pendingExits.Add(exit);
                continue;
            }

            int length = Random.Range(corridorLengthMin, corridorLengthMax + 1);
            Transform currentExit = exit;
            bool branchBuilt = false;

            for (int step = 0; step < length; step++)
            {
                // Выбираем тип комнаты для этого шага
                GameObject prefab = GetRandomCorridorOrSpecial();

                GameObject newRoom = SpawnRoomAtExit(prefab, currentExit);
                if (newRoom != null)
                {
                    spawnedRooms.Add(newRoom);
                    branchBuilt = true;

                    // Получаем следующий выход для продолжения цепочки
                    Transform nextExit = GetOppositeExit(newRoom, currentExit);
                    if (nextExit != null)
                        currentExit = nextExit;
                    else
                        break; // Нет продолжения (тупик)
                }
                else break;
            }

            if (branchBuilt) builtBranches++;
            else pendingExits.Add(exit); // Если не получилось начать, вернем в очередь на тупик
        }

        Debug.Log($"Построено ответвлений: {builtBranches}");

        // 5. Закрываем ВСЕ оставшиеся свободные выходы тупиками
        CloseAllOpenExits();

        Debug.Log($"=== ГОТОВО === Комнат: {spawnedRooms.Count}");

        // 6. Размещаем игрока
        SpawnPlayer(startRoom);
    }

    bool ValidatePrefabs()
    {
        if (roomStart == null) Debug.LogError("Room Start не назначен!");
        if (hallMain == null) Debug.LogError("Hall Main не назначен!");
        if (miniCorridor == null) Debug.LogError("Mini Corridor не назначен!");
        if (miniTupik == null) Debug.LogError("Mini Tupik не назначен!");
        return roomStart != null && hallMain != null && miniCorridor != null && miniTupik != null;
    }

    GameObject GetRandomCorridorOrSpecial()
    {
        float r = Random.value;
        if (r < 0.3f && miniCorridor != null) return miniCorridor;
        if (r < 0.6f && corridorLong != null) return corridorLong;
        if (r < 0.8f && corridorVeryLong != null) return corridorVeryLong;
        if (corridorVeryLong2 != null) return corridorVeryLong2;
        return miniCorridor != null ? miniCorridor : corridorLong;
    }

    /// <summary>
    /// Получает выход комнаты, противоположный тому, которым мы присоединились.
    /// Ищем выход с направлением, близким к обратному направлению usedExit.
    /// </summary>
    Transform GetOppositeExit(GameObject room, Transform usedExit)
    {
        Vector3 usedDir = usedExit.forward; // Мировое направление использованного выхода
        Transform exitsParent = room.transform.Find("Exits");
        if (exitsParent == null) return null;

        Transform bestExit = null;
        float bestDot = -1f;
        foreach (Transform child in exitsParent)
        {
            if (!child.name.StartsWith("Exit_")) continue;
            Vector3 childDir = child.forward; // Направление этого выхода в мире
            float dot = Vector3.Dot(childDir, -usedDir); // Ищем сонаправленный с обратным usedExit
            if (dot > bestDot && child != usedExit) // Исключаем тот же самый выход
            {
                bestDot = dot;
                bestExit = child;
            }
        }
        return bestExit;
    }

    /// <summary>
    /// Ищет выход комнаты по типу (North/South/East/West) на основе локальной позиции.
    /// </summary>
    Transform GetExitByType(GameObject room, string direction)
    {
        Transform exitsParent = room.transform.Find("Exits");
        if (exitsParent == null) return null;
        foreach (Transform child in exitsParent)
        {
            if (!child.name.StartsWith("Exit_")) continue;
            Vector3 localPos = child.localPosition;
            if (direction == "North" && localPos.z > 1f && Mathf.Abs(localPos.z) > Mathf.Abs(localPos.x)) return child;
            if (direction == "South" && localPos.z < -1f && Mathf.Abs(localPos.z) > Mathf.Abs(localPos.x)) return child;
            if (direction == "East" && localPos.x > 1f && Mathf.Abs(localPos.x) > Mathf.Abs(localPos.z)) return child;
            if (direction == "West" && localPos.x < -1f && Mathf.Abs(localPos.x) > Mathf.Abs(localPos.z)) return child;
        }
        return null;
    }

    void CollectExits(GameObject room)
    {
        Transform exitsParent = room.transform.Find("Exits");
        if (exitsParent == null) return;
        foreach (Transform child in exitsParent)
        {
            if (child.name.StartsWith("Exit_"))
                pendingExits.Add(child);
        }
    }

    void CloseAllOpenExits()
    {
        // Используем универсальный тупик (miniTupik), а также secret и exit
        List<Transform> exitsToClose = new List<Transform>(pendingExits);
        foreach (Transform exit in exitsToClose)
        {
            if (exit == null) continue;
            // Выбираем, какой тупик поставить: обычный, секрет или выход
            GameObject deadEndPrefab = miniTupik;
            if (roomSecret != null && Random.Range(0, 100) < secretRoomChance)
                deadEndPrefab = roomSecret;
            else if (roomExit != null && spawnedRooms.Count > 5 && Random.Range(0, 100) < 10)
                deadEndPrefab = roomExit;

            if (SpawnRoomAtExit(deadEndPrefab, exit) != null)
            {
                pendingExits.Remove(exit);
            }
            else
            {
                // Если не получилось, пробуем обычный тупик
                if (deadEndPrefab != miniTupik && SpawnRoomAtExit(miniTupik, exit) != null)
                    pendingExits.Remove(exit);
            }
        }
        Debug.Log($"Осталось незакрытых выходов: {pendingExits.Count}");
    }

    /// <summary>
    /// Универсальный метод стыковки комнаты к заданному выходу.
    /// </summary>
    GameObject SpawnRoomAtExit(GameObject prefab, Transform targetExit)
    {
        if (prefab == null || targetExit == null) return null;

        Vector3 targetDir = targetExit.forward; // Мировое направление, куда смотрит целевой выход

        // Получаем все выходы префаба
        Transform[] allChildren = prefab.GetComponentsInChildren<Transform>();
        List<Transform> prefabExits = new List<Transform>();
        foreach (Transform t in allChildren)
            if (t.name.StartsWith("Exit_")) prefabExits.Add(t);

        if (prefabExits.Count == 0) return null;

        // Пробуем каждый выход префаба
        foreach (Transform prefabExit in prefabExits)
        {
            // Локальное направление этого выхода (предполагаем, что ось Z смотрит наружу)
            Vector3 localDir = prefabExit.localPosition.normalized;
            // Убираем вертикальную составляющую
            localDir.y = 0;
            if (localDir.magnitude < 0.1f) continue;
            localDir.Normalize();

            // Требуемый поворот: чтобы localDir стал равен -targetDir
            Quaternion rotation = Quaternion.FromToRotation(localDir, -targetDir);
            // Позиция новой комнаты
            Vector3 position = targetExit.position - rotation * prefabExit.localPosition;

            // Проверка наложения
            if (IsPositionOccupied(position, prefab)) continue;

            // Создаём комнату
            GameObject room = Instantiate(prefab, position, rotation);
            room.transform.parent = generatorRoot;
            room.name = prefab.name + "_" + roomCounter;
            roomCounter++;
            spawnedRooms.Add(room);

            // Убираем целевой выход из pendingExits (он больше не свободен)
            pendingExits.Remove(targetExit);

            // Добавляем все выходы новой комнаты, кроме того, которым состыковались
            Transform newExits = room.transform.Find("Exits");
            if (newExits != null)
            {
                foreach (Transform child in newExits)
                {
                    if (child.name.StartsWith("Exit_") &&
                        Vector3.Distance(child.position, targetExit.position) > 0.5f) // Не добавляем состыкованный
                    {
                        pendingExits.Add(child);
                    }
                }
            }

            if (prefab == roomMonsterLair)
                monsterLairCount++;

            return room;
        }

        return null;
    }

    GameObject SpawnRoom(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (IsPositionOccupied(position, prefab)) return null;
        GameObject room = Instantiate(prefab, position, rotation);
        room.transform.parent = generatorRoot;
        room.name = prefab.name + "_" + roomCounter;
        roomCounter++;
        spawnedRooms.Add(room);
        return room;
    }

    bool IsPositionOccupied(Vector3 position, GameObject ignorePrefab = null)
    {
        Collider[] colliders = Physics.OverlapSphere(position, overlapCheckRadius);
        foreach (Collider col in colliders)
        {
            if (col.transform.IsChildOf(generatorRoot))
                return true;
        }
        return false;
    }

    void SpawnPlayer(GameObject startRoom)
    {
        Transform spawnPoint = startRoom.transform.Find("PlayerSpawn");
        if (spawnPoint == null) spawnPoint = startRoom.transform;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = spawnPoint.position + Vector3.up * 2.5f;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = false;
            }
            Debug.Log("✅ Игрок заспавнен!");
        }
    }

    void ClearLevel()
    {
        foreach (Transform child in generatorRoot)
            Destroy(child.gameObject);
        spawnedRooms.Clear();
        pendingExits.Clear();
        monsterLairCount = 0;
        roomCounter = 0;
    }

    [ContextMenu("Сгенерировать уровень")]
    void GenerateInEditor() => GenerateLevel();

    [ContextMenu("Очистить уровень")]
    void ClearInEditor() => ClearLevel();
}