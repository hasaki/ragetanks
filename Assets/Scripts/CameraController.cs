using System;
using UnityEngine;

namespace RageTanks
{
	public class CameraController : MonoBehaviour
	{	
		private PlayerState _currentState = PlayerState.Alive;
		private Vector3 _lastTargetPosition = Vector3.zero;
		private Vector3 _currentTargetPosition = Vector3.zero;
		private float _currentLerpDistance = 0.0f;

		public CameraController()
		{
			CameraTrackingSpeed = 0.2f;
		}

		public GameObject Player;
		public float CameraTrackingSpeed;
		public float PlayerVerticalOffset = 1f;

		// Use this for initialization
		void Start()
		{
			var playerPosition = Player.transform.position;
			var cameraPosition = transform.position;
			var startingTargetPosition = playerPosition;

			startingTargetPosition.z = cameraPosition.z;
			_lastTargetPosition = startingTargetPosition;
			_currentTargetPosition = startingTargetPosition;
			_currentLerpDistance = 1.0f;
		}

		void OnEnable()
		{
		}

		void OnDisable()
		{
		}

		void OnPlayerStateChange(PlayerState newState)
		{
			_currentState = newState;
		}

		void LateUpdate()
		{
			OnStateCycle();

			_currentLerpDistance += CameraTrackingSpeed;
			transform.position = Vector3.Lerp(_lastTargetPosition, _currentTargetPosition, _currentLerpDistance);
		}

		private void OnStateCycle()
		{
			// Track the player....or not
			switch (_currentState)
			{
				case PlayerState.Resurrecting:
				case PlayerState.Alive:
					TrackPlayer();
					break;
				case PlayerState.Dead:
					StopTrackingPlayer();
					break;
				default:
					StopTrackingPlayer();
					break;
			}
		}

		private void TrackPlayer()
		{
			Func<float, float, bool> isEqual = (left, right) => Math.Abs(left - right) <= float.Epsilon;

			var currentCameraPosition = transform.position;
			var currentPlayerPosition = Player.transform.position;
			currentPlayerPosition.y += PlayerVerticalOffset;

			if (isEqual(currentCameraPosition.x, currentPlayerPosition.x) &&
				isEqual(currentCameraPosition.y, currentPlayerPosition.y))
			{
				_currentLerpDistance = 1f;
				_lastTargetPosition = currentCameraPosition;
				_currentTargetPosition = currentCameraPosition;
				return;
			}

			_currentLerpDistance = 0f;
			_lastTargetPosition = currentCameraPosition;
			_currentTargetPosition = currentPlayerPosition;
			_currentTargetPosition.z = currentCameraPosition.z;
		}

		private void StopTrackingPlayer()
		{
			var currentCameraPosition = transform.position;

			_currentTargetPosition = _lastTargetPosition = currentCameraPosition;

			_currentLerpDistance = 1f;
		}
	}
}
