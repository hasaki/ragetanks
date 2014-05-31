using UnityEngine;

namespace RageTanks
{
	public class RandomSizingBehavior : MonoBehaviour
	{
		public float minSize = 0.75f;
		public float maxSize = 2.5f;

		// Use this for initialization
		void Start()
		{
			var scale = gameObject.transform.localScale;

			var size = Random.Range(minSize, maxSize);

			scale.x = size;
			scale.y = size;

			gameObject.transform.localScale = scale;
		}
	}
}
