using UnityEngine;
using System.Collections;

public class DestroyOnCollision : MonoBehaviour
{
	void OnTriggerEnter2D(Collider2D hitObj)
	{
		if (hitObj.tag == "Platform")
		{
			DestroyObject(gameObject);
		}
	}
}
