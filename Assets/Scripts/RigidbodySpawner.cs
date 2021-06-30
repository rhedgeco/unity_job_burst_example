using System.Collections.Generic;
using UnityEngine;

public class RigidbodySpawner : MonoBehaviour {
    [SerializeField] private Rigidbody[] rigidbodyPrefab;
    [SerializeField] private float spawnFrequency = 1;
    [SerializeField] private int maxSpheres = 32;
    [SerializeField] private float despawnHeight = -10;

    private float _timer;
    private List<GameObject> _objects = new List<GameObject>();

    private void Update() {
        if (rigidbodyPrefab.Length == 0) return;
        
        DestroyObjects();
        _timer += Time.deltaTime;
        while (_timer > spawnFrequency && _objects.Count < maxSpheres) {
            _timer -= spawnFrequency;
            Rigidbody prefab = rigidbodyPrefab[Random.Range(0, rigidbodyPrefab.Length)];
            GameObject sphere = Instantiate(prefab.gameObject, transform);
            sphere.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            _objects.Add(sphere);
        }
    }

    private void DestroyObjects() {
        for (int i = _objects.Count - 1; i >= 0; i--) {
            if (_objects[i].transform.position.y <= despawnHeight) {
                Destroy(_objects[i]);
                _objects.RemoveAt(i);
            }
        }
    }
}