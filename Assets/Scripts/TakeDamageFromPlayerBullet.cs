using RageTanks.Annotations;
using UnityEngine;

namespace RageTanks
{
	public class TakeDamageFromPlayerBullet : MonoBehaviour
	{
		public delegate void HitByPlayerBullet();
		public event HitByPlayerBullet HitByBullet;

		[UsedImplicitly]
		void OnTriggerEnter2D(Collider2D collidedObject)
		{
			if (collidedObject.tag == "PlayerBullet")
			{
				OnHitByPlayerBullet();
			}
		}

		protected void OnHitByPlayerBullet()
		{
			if (HitByBullet != null)
				HitByBullet();
		}
	}
}
