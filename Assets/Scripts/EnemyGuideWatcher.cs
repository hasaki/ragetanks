using UnityEngine;
using System.Collections;

public class EnemyGuideWatcher : MonoBehaviour
{
	private int _enemyLayer = 0;
	public BasicEnemyController enemyObject;

	void Start()
	{
		_enemyLayer = UnityEngine.LayerMask.NameToLayer("Enemy");
	}

	void OnTriggerEnter2D(Collider2D obj)
	{
		if(obj.gameObject.layer == _enemyLayer)
			enemyObject.SwitchDirections();
	}

	void OnTriggerExit2D(Collider2D obj)
	{
		if (obj.tag == "Platform")
			enemyObject.SwitchDirections();
	}
}
