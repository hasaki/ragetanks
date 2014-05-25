using System;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[UsedImplicitly]
public class PlayerController : MonoBehaviour
{
	private enum PlayerTimers
	{
		Fire,
		_MaxTimer
	}

	public Transform bulletSpawnPoint;
	public GameObject bulletPrefab = null;
	public GameObject playerRespawnPoint = null;
	public float playerJumpForceVertical = 250f;
	public float fireDelay = 0.25f;

	private const float PlayerWalkSpeed = 3f;

	private const int MaxTimers = (int) PlayerTimers._MaxTimer;
	private float [] _timers;
	
	private PlayerState _currentState = PlayerState.Alive;
	private Animator _playerAnimator = null;
	private bool _playerHasLanded = true;

	void Start()
	{
		FacingRight = true;

		_timers = new float[MaxTimers];
		for (int i = 0; i < MaxTimers; i++)
			_timers[i] = Time.time;
	}

	public bool FacingRight { get; set; }

	void Update()
	{
		ProcessPlayerInput(InputController.Instance.Input);
		ProcessCurrentState();
	}

	void OnEnable()
	{
		_playerAnimator = GetComponent<Animator>();
	}

	void OnDisable()
	{
	}

	void ProcessPlayerInput(PlayerInput input)
	{
		var moving = input.HasFlag(PlayerInput.Left | PlayerInput.Right);

		if (moving)
		{
			bool nowFacingRight = false;

			if (input.HasFlag(PlayerInput.Left))
			{
				transform.Translate(new Vector3(PlayerWalkSpeed * -1 * Time.deltaTime, 0f, 0f));
				nowFacingRight = false;
			}
			else // PlayerInput.Right
			{
				transform.Translate(new Vector3(PlayerWalkSpeed * Time.deltaTime, 0f, 0f));
				nowFacingRight = true;
			}

			if (nowFacingRight != FacingRight)
			{
				FacingRight = nowFacingRight;
				SetSpriteDirection();
			}
		}

		if (input.HasFlag(PlayerInput.Jump) && _playerHasLanded)
			Jump();

		if (input.HasFlag(PlayerInput.Fire))
			Fire();
		
		SetAnimationState(input);
	}

	void ProcessCurrentState()
	{
		switch (_currentState)
		{
			case PlayerState.Dead:
				ChangeState(PlayerState.Resurrecting);
				break;
			case PlayerState.Resurrecting:
				transform.position = playerRespawnPoint.transform.position;
				transform.rotation = Quaternion.identity;
				rigidbody2D.velocity = Vector2.zero;
				rigidbody2D.angularVelocity = 0;
				ChangeState(PlayerState.Alive);
				break;
		}
	}

	void ChangeState(PlayerState state)
	{
		// If the state is the same, exit early
		if (state == _currentState)
			return;

		if (!CanTransitionToState(state))
			return;

		_currentState = state;
	}

	private void SetAnimationState(PlayerInput input)
	{
		switch (input)
		{
			case PlayerInput.Left:
			case PlayerInput.Right:
				_playerAnimator.SetBool("Walking", true);
				break;
			default:
				_playerAnimator.SetBool("Walking", false);
				break;
		}
	}

	private void SetSpriteDirection()
	{
		var scaleX = FacingRight ? 1f : -1f;

		var scale = transform.localScale;
		scale.x = Math.Abs(scale.x) * scaleX;
		transform.localScale = scale;
	}

	private void Jump()
	{
		rigidbody2D.AddForce(new Vector2(0, playerJumpForceVertical));
		_playerHasLanded = false;
	}

	private void Fire()
	{
		if (!CanFireBullet)
			return;

		var newBullet = (GameObject) Instantiate(bulletPrefab);
		newBullet.transform.position = bulletSpawnPoint.position;

		var bulletController = newBullet.GetComponent<PlayerBulletController>();
		bulletController.playerObject = gameObject;
		bulletController.LaunchBullet();

		_timers[(int) PlayerTimers.Fire] = Time.time + fireDelay;
	}

	private bool CanFireBullet
	{
		get { return Time.time >= _timers[(int) PlayerTimers.Fire]; }
	}

	[UsedImplicitly]
	public void DeathTriggerHit()
	{
		ChangeState(PlayerState.Dead);
	}

	[UsedImplicitly]
	public void PlayerLanded()
	{
		_playerHasLanded = true;
	}

	[UsedImplicitly]
	public void PlayerFalling()
	{
		_playerHasLanded = false;
	}

	private bool CanTransitionToState(PlayerState newState)
	{
		switch (_currentState)
		{
			case PlayerState.Alive:
				return true;
			case PlayerState.Dead:
				return newState == PlayerState.Resurrecting;
			case PlayerState.Resurrecting:
				return newState == PlayerState.Alive;
		}

		return true;
	}
}
