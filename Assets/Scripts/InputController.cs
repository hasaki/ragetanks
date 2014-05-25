using UnityEngine;
using System;
using System.Collections;

public class InputController : MonoBehaviour
{
	public static InputController Instance { get; private set; }
	
	void OnEnable()
	{
		Instance = this;
	}

	public PlayerInput Input { get; private set; }

	void Update()
	{
		var input = PlayerInput.None;

		// Detect the current input of the Horizontal axis, then
		// broadcast a state update for the player as needed.
		// Do this on each frame to make sure the state is always
		// set properly based on the current user input.
		var horizontal = UnityEngine.Input.GetAxis("Horizontal");
		if(Math.Abs(horizontal) > float.Epsilon)
		{
			input |= horizontal < 0 ? PlayerInput.Left : PlayerInput.Right;
		}

		var jump = UnityEngine.Input.GetButton("Jump");
		if (jump)
		{
			input |= PlayerInput.Jump;
		}

		var fire = UnityEngine.Input.GetButton("Fire1");
		if (fire)
		{
			input |= PlayerInput.Fire;
		}

		Input = input;
	}
}
