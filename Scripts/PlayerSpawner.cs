using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public string startRoomName = "Room_Start_0";
    public string spawnPointName = "PlayerSpawn";

    void Start()
    {
        StartCoroutine(SpawnPlayer());
    }

    System.Collections.IEnumerator SpawnPlayer()
    {
        yield return null;

        GameObject startRoom = GameObject.Find(startRoomName);

        if (startRoom != null)
        {
            Transform spawnPoint = startRoom.transform.Find(spawnPointName);
            
            if (spawnPoint != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = spawnPoint.position;
                    player.transform.rotation = spawnPoint.rotation;
                    Debug.Log("✅ Игрок заспавнен!");
                }
                else
                {
                    Debug.LogError("Игрок с тегом Player не найден!");
                }
            }
        }
    }
}