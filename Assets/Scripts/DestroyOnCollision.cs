using UnityEngine;
using System.Collections;

public class DestroyOnCollision : MonoBehaviour
{
	void OnTriggerEnter2D(Collider2D hitObj)
	{
		DestroyObject(gameObject);
	}
}
