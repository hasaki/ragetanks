using UnityEngine;
using System.Collections;

public class DeathTrigger : MonoBehaviour
{
	private int _playerLayer;

	void Start()
	{
		_playerLayer = UnityEngine.LayerMask.NameToLayer("Player");
	}

	void OnTriggerEnter2D(Collider2D collideObject)
	{
		if (collideObject.gameObject.layer == _playerLayer)
		{
			collideObject.SendMessage("DeathTriggerHit", SendMessageOptions.DontRequireReceiver);
		}
	}
}
