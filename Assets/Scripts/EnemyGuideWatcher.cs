using RageTanks.Annotations;
using UnityEngine;

namespace RageTanks
{
	public class EnemyGuideWatcher : MonoBehaviour
	{
		private int _enemyLayer = 0;
		public BasicEnemyController enemyObject;

		[UsedImplicitly]
		void Start()
		{
			_enemyLayer = UnityEngine.LayerMask.NameToLayer("Enemy");
		}

		[UsedImplicitly]
		void OnTriggerEnter2D(Collider2D obj)
		{
			if(obj.gameObject.layer == _enemyLayer)
				enemyObject.SwitchDirections();
		}

		[UsedImplicitly]
		void OnTriggerExit2D(Collider2D obj)
		{
			if (obj.tag == "Platform")
				enemyObject.SwitchDirections();
		}
	}
}
