using RageTanks.Annotations;
using UnityEngine;

namespace RageTanks
{
	public class PlayerColliderListener : MonoBehaviour
	{
		[UsedImplicitly]
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

		[UsedImplicitly]
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
}
