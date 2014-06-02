using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace RageTanks.Enemy
{
	public class BossEnemyController : BaseEnemyController
	{
		public enum BossState
		{
			Idle,
			FallingToNode,
			WaitingToJump,
			WaitingToFall,
			JumpingOffPlatform
		}

		public Transform inActiveNode = null;
		public Transform dropToStartNode = null;
		public Transform dropFXSpawnPoint = null;
		public List<Transform> dropNodeList = new List<Transform>();
		public GameObject bossDeathFX = null;
		public GameObject bossDropFX = null;

		public TakeDamageFromPlayerBullet bulletCollider = null;

		public int health = 20;
		public float moveSpeed = 0.1f;
		public float eventWaitDelay = 3f;

		public int enemiesToStartBattle = 10;

		public BossState currentState = BossState.Idle;

		private Transform _targetNode = null;
		private float _timeForNextEvent = 0f;
		private Vector3 _targetPosition = Vector3.zero;

		private int _startHealth = 20;

		private bool _isDead = false;
		private int _enemiesLeftToKill = 0;

		public BossEnemyController()
		{
			score = 1000;
		}

		void OnEnable()
		{
			bulletCollider.HitByBullet += WasHitByPlayerBullet;
			BasicEnemyController.EnemyDied += EnemyHasDied;
		}

		void OnDisable()
		{
			bulletCollider.HitByBullet -= WasHitByPlayerBullet;
			BasicEnemyController.EnemyDied -= EnemyHasDied;
		}

		void Start()
		{
			transform.position = inActiveNode.position;
			_enemiesLeftToKill = enemiesToStartBattle;
		}

		void Update()
		{
			switch (currentState)
			{
				case BossState.Idle:
					break;
				case BossState.FallingToNode:
					if (transform.position.y > _targetNode.position.y)
					{
						transform.Translate(new Vector3(0f, -moveSpeed * Time.deltaTime, 0f));

						if (transform.position.y < _targetNode.position.y)
						{
							transform.position = _targetNode.position;
						}
					}
					else
					{
						CreateDropFX();

						_timeForNextEvent = 0f;
						currentState = BossState.WaitingToJump;
					}
					break;
				case BossState.WaitingToFall:
					if (_timeForNextEvent < float.Epsilon)
					{
						_timeForNextEvent = Time.time + eventWaitDelay;
					}
					else if (_timeForNextEvent < Time.time)
					{
						_targetNode = dropNodeList[Random.Range(0, dropNodeList.Count)];

						transform.position = GetSkyPositionOfNode(_targetNode);

						currentState = BossState.FallingToNode;
						_timeForNextEvent = 0f;
					}
					break;
				case BossState.WaitingToJump:
					if (_timeForNextEvent < float.Epsilon)
					{
						_timeForNextEvent = Time.time + eventWaitDelay;
					}
					else
					{
						_targetPosition = GetSkyPositionOfNode(_targetNode);
						currentState = BossState.JumpingOffPlatform;
						_timeForNextEvent = 0f;

						_targetNode = null;
					}
					break;
				case BossState.JumpingOffPlatform:
					if (transform.position.y < _targetPosition.y)
					{
						transform.Translate(new Vector3(0f, moveSpeed*Time.deltaTime, 0f));

						if (transform.position.y > _targetPosition.y)
							transform.position = _targetPosition;
					}
					else
					{
						_timeForNextEvent = 0f;
						currentState = BossState.WaitingToFall;
					}
					break;
			}
		}

		public void BeginBossBattle()
		{
			_targetNode = dropToStartNode;
			currentState = BossState.FallingToNode;

			_timeForNextEvent = 0f;
			health = _startHealth;
			_isDead = false;
		}

		private Vector3 GetSkyPositionOfNode(Transform node)
		{
			var target = node.position;

			target.y += 9f;

			return target;
		}

		private void WasHitByPlayerBullet()
		{
			health -= 1;

			if (health <= 0)
				KillBoss();
		}

		private void CreateDropFX()
		{
			var dropFxParticle = (GameObject) Instantiate(bossDropFX);
			dropFxParticle.transform.position = dropFXSpawnPoint.transform.position;
		}

		private void KillBoss()
		{
			if (_isDead)
				return;

			_isDead = true;
			var deathFxParticle = (GameObject) Instantiate(bossDeathFX);
			deathFxParticle.transform.position = dropFXSpawnPoint.transform.position;

			OnEnemyDied(EnemyType.Boss);

			transform.position = inActiveNode.position;
			currentState = BossState.Idle;
			_timeForNextEvent = 0f;
			_enemiesLeftToKill = enemiesToStartBattle;
		}

		private void EnemyHasDied(int s, EnemyType type)
		{
			if (currentState == BossState.Idle)
			{
				if (type != EnemyType.Boss)
				{
					_enemiesLeftToKill--;

					if (_enemiesLeftToKill <= 0)
					{
						BeginBossBattle();
					}
				}
			}
		}
	}
}
// ReSharper restore FieldCanBeMadeReadOnly.Global
