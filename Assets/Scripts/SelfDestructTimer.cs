using UnityEngine;

namespace Assets.Scripts
{
	public class SelfDestructTimer : MonoBehaviour
	{
		public float Delay = 1f;

		private float DestroyAtTime = 0f;

		void Start()
		{
			DestroyAtTime = Delay + Time.time;
		}

		void Update()
		{
			if (Time.time >= DestroyAtTime)
			{
				Destroy(gameObject);
			}
		}

		private float CalculateDestroyTime()
		{
			return 0;
		}
	}
}
