using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemiesSpawn_Controller : MonoBehaviour
{
    public bool canSpawn = true; // 1
    public GameObject EnemyPrefab; // 2
    public List<Transform> EnemiesSpawnPositions = new List<Transform>(); // 3
    public float timeBetweenSpawns_Min;
    public float timeBetweenSpawns_Max;
    private int level = 0;
    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        StartCoroutine(SpawnRoutine());
    }

    private void Update()
    {
        if(gameManager.Get_Seconds() == 30 && level < 3 && level == 0)
        {
            timeBetweenSpawns_Min /= 2;
            timeBetweenSpawns_Max /= 2;
            level++;
        }
        if (gameManager.Get_Seconds() == 1 && level < 3 && level == 1)
        {
            timeBetweenSpawns_Min /= 2;
            timeBetweenSpawns_Max /= 2;
            level++;
        }
        if (gameManager.Get_Seconds() == 45 && level < 3 && level == 2)
        {
            timeBetweenSpawns_Min /= 2;
            timeBetweenSpawns_Max /= 2;
            level++;
        }
    }

    private void SpawnEnemy()
    {
        Vector3 randomPosition = EnemiesSpawnPositions[Random.Range(0,
       EnemiesSpawnPositions.Count)].position; // 1
        GameObject enemy = Instantiate(EnemyPrefab, randomPosition,
       EnemyPrefab.transform.rotation); // 2
    }

    private IEnumerator SpawnRoutine() // 1
    {
        while (canSpawn) // 2
        {
            SpawnEnemy(); // 3
            yield return new WaitForSeconds(Random.Range(timeBetweenSpawns_Min, timeBetweenSpawns_Max)); // 4
        }
    }
}
