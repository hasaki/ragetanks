using System;
using UnityEngine;

namespace RageTanks
{
	public abstract class BaseEnemyController : MonoBehaviour
	{
		public static event Action<int> EnemyDied;

		public int score;

		public BaseEnemyController()
		{
			score = 25;
		}

		protected void OnEnemyDied()
		{
			if (EnemyDied != null)
				EnemyDied(score);
		}

		void OnCollideEnter2D(Collider2D obj)
		{
			if (obj.tag == "Platform")
			{
				rigidbody2D.isKinematic = true;
			}
		}
	}
}
