using System;
using RageTanks.Annotations;
using UnityEngine;

namespace RageTanks.Enemy
{
	public abstract class BaseEnemyController : MonoBehaviour
	{
		public static event Action<int, EnemyType> EnemyDied;

		public int score;

		public BaseEnemyController()
		{
			score = 25;
		}

		protected void OnEnemyDied(EnemyType type)
		{
			if (EnemyDied != null)
				EnemyDied(score, type);
		}

		[UsedImplicitly]
		void OnCollideEnter2D(Collider2D obj)
		{
			if (obj.tag == "Platform")
			{
				rigidbody2D.isKinematic = true;
			}
		}
	}
}
