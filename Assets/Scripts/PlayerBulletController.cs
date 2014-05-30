using RageTanks;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class PlayerBulletController : MonoBehaviour
{
	private const float Lifetime = 1f;

	public GameObject playerObject = null;
	public float bulletSpeed = 15f;

	private float _destroyAt = 0f;

	// Update is called once per frame
	void Update()
	{
		if(_destroyAt > 0 && Time.time >= _destroyAt)
			Destroy(gameObject);
	}

	public void LaunchBullet()
	{
		var playerController = playerObject.GetComponent<PlayerController>();

		var bulletForce = new Vector2(bulletSpeed * (playerController.FacingRight ? 1f : -1f), 0f);

		rigidbody2D.velocity = bulletForce;
		_destroyAt = Time.time + Lifetime;
	}
}
