using UnityEngine;


public class RandomItemCreator : MonoBehaviour
{
	public GameObject[] prefabs;
	public GameObject bounds;
	public GameObject[] createdObjects;
	public int numObjects;

	void Start()
	{
		if (bounds != null && bounds.renderer && prefabs != null && prefabs.Length > 0)
		{
			createdObjects = new GameObject[numObjects];
			for (var index = 0; index < numObjects; index++)
			{
				var prefab = prefabs[Random.Range(0, prefabs.Length - 1)];
				var extents = bounds.renderer.bounds.extents;
				var pos = bounds.transform.position;
				pos.x = Random.Range(0f, extents.x) + pos.x - extents.x / 2f;
				pos.y = Random.Range(0f, extents.y) + pos.y - extents.y / 2f;

				var createdObject = (GameObject) Instantiate(prefab, pos, Quaternion.identity);
				createdObjects[index] = createdObject;
			}

			bounds.renderer.enabled = false;
		}
	}
}

