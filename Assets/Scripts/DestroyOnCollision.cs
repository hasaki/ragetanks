using RageTanks.Annotations;
using UnityEngine;

namespace RageTanks
{
	public class DestroyOnCollision : MonoBehaviour
	{
		[UsedImplicitly]
		void OnTriggerEnter2D(Collider2D hitObj)
		{
			if (hitObj.tag == "Platform")
				DestroyObject(gameObject);
			else if (hitObj.tag == "Enemy")
				Destroy(gameObject, 0.01f); // Destroy the bullet after its hit the enemy
		}
	}
}
