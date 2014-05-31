using RageTanks.Annotations;
using UnityEngine;

namespace RageTanks
{
	public class EnemySpawner : MonoBehaviour
	{
		enum RespawnState
		{
			Idle,
			Revving,
			Spawning,
		}

		// ReSharper disable FieldCanBeMadeReadOnly.Global
		public GameObject prefab = null;
		// ReSharper restore FieldCanBeMadeReadOnly.Global

		private float _time = 0f;
		private GameObject _enemy = null;
		private RespawnState _state = RespawnState.Idle;

		void Start()
		{
			renderer.enabled = false;
		}

		[UsedImplicitly]
		void Update()
		{
			switch (_state)
			{
				case RespawnState.Idle:
					if (_enemy == null)
					{
						RevSpawner();
					}
					break;
				case RespawnState.Revving:
					if (Time.time > _time)
					{
						ScheduleRespawn();
					}
					break;
				case RespawnState.Spawning:
					if (Time.time > _time)
					{
						SpawnEnemy();
					}
					break;
			}
		}

		private void RevSpawner()
		{
			_state = RespawnState.Revving;
			_time = Time.time + Random.Range(5f, 10f);
		}

		void SpawnEnemy()
		{
			if (prefab != null)
			{
				_enemy = (GameObject) Instantiate(prefab);
				_enemy.transform.position = transform.position;
				_time = 0f;
			}

			_state = RespawnState.Idle;
			_time = 0f;

			renderer.enabled = false;
		}

		void ScheduleRespawn()
		{
			_time = Time.time + Random.Range(1f, 5f);
			renderer.enabled = true;

			_state = RespawnState.Spawning;
		}
	}
}
