using UnityEngine;
using System.Collections;

public class PlayerColliderListener : MonoBehaviour
{
	void OnTriggerEnter2D(Collider2D collidedObject)
	{
		switch (collidedObject.tag)
		{
			case "Platform":
				this.SendMessageUpwards("PlayerLanded", SendMessageOptions.DontRequireReceiver);
				break;
			case "DeathTrigger":
				SendMessageUpwards("DeathTriggerHit", SendMessageOptions.DontRequireReceiver);
				break;
		}
	}

	void OnTriggerExit2D(Collider2D leftObject)
	{
		switch (leftObject.tag)
		{
			case "Platform":
				SendMessageUpwards("PlayerFalling", SendMessageOptions.DontRequireReceiver);
				break;
		}
	}
}
