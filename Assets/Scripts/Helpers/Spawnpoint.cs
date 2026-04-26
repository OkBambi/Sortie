using UnityEngine;

public class Spawnpoint : MonoBehaviour
{
    [SerializeField] GameObject spawnObject;
    [Space]
    [Tooltip("Spawns the object over and over a period after it dies")]
    [SerializeField] bool recurringSpawn;
    [SerializeField] float timeBetweenSpawns;

    private bool isSpawning; //basically a debounce
    private GameObject currentlySpawnedObject;

    void Start()
    {
        currentlySpawnedObject = transform.childCount > 0 ? transform.GetChild(0).gameObject : null;
    }

    void Update()
    {
        //bad to keep in update, preferably we listen for the die delegate
        if (currentlySpawnedObject == null && !isSpawning)
        {
            isSpawning = true;
            Invoke("SpawnObject", timeBetweenSpawns);
        }
    }

    void SpawnObject()
    {
        GameObject s = Instantiate(spawnObject);
        s.transform.SetParent(transform, false);
        currentlySpawnedObject = s;
        isSpawning = false;
    }
}
