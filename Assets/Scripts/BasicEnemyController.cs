using System;
using RageTanks.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RageTanks
{
	public class BasicEnemyController : BaseEnemyController
	{
		public ParticleSystem DeathFxParticlePrefab = null;
		public TakeDamageFromPlayerBullet bulletColliderListener = null;
		public float walkingSpeed = 0.45f;

		private bool _walkingLeft = true;
		private bool alive = true;

		[UsedImplicitly]
		void Start()
		{
			_walkingLeft = Random.Range(0, 2) == 1;
			UpdateOrientation();
		}

		[UsedImplicitly]
		void OnEnable()
		{
			bulletColliderListener.HitByBullet += HitByPlayerBullet;
		}

		[UsedImplicitly]
		void OnDisable()
		{
			bulletColliderListener.HitByBullet -= HitByPlayerBullet;
		}

		[UsedImplicitly]
		void Update()
		{
			transform.Translate(new Vector3(walkingSpeed * Time.deltaTime * (_walkingLeft ? -1f : 1f), 0f, 0f));
		}

		private void UpdateOrientation()
		{
			var localScale = transform.localScale;

			localScale.x = Math.Abs(localScale.x) * (_walkingLeft ? -1f : 1f);
			transform.localScale = localScale;
		}

		public void SwitchDirections()
		{
			_walkingLeft = !_walkingLeft;
			UpdateOrientation();
		}

		public void HitByPlayerBullet()
		{
			Die();
		}

		private void Die()
		{
			if (!alive)
				return;
		
			alive = false;

			var deathFx = Instantiate(DeathFxParticlePrefab) as ParticleSystem;

			if (deathFx != null)
			{
				var enemyPos = transform.position;
				var particlePostion = new Vector3(enemyPos.x, enemyPos.y, enemyPos.z + 1f);
				deathFx.transform.position = particlePostion;
			}

			OnEnemyDied();

			Destroy(gameObject, 0.1f);
		}
	}
}
