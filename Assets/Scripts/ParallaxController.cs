using UnityEngine;

namespace RageTanks
{
	public class ParallaxController : MonoBehaviour
	{
		public GameObject[] clouds;
		public GameObject[] nearHills;
		public GameObject[] farHills;

		public float cloudLayerSpeedModifier;
		public float nearHillsLayerSpeedModifier;
		public float farHillsLayerSpeedModifier;

		public Camera playerCamera;

		private Vector3 _lastCameraPosition;

		void Start()
		{
			_lastCameraPosition = playerCamera.transform.position;
		}

		void Update()
		{
			var currentPosition = playerCamera.transform.position;
			var xPosDiff = _lastCameraPosition.x - currentPosition.x;

			AdjustParalaxPositionsForArray(clouds, cloudLayerSpeedModifier, xPosDiff);
			AdjustParalaxPositionsForArray(nearHills, nearHillsLayerSpeedModifier, xPosDiff);
			AdjustParalaxPositionsForArray(farHills, farHillsLayerSpeedModifier, xPosDiff);

			_lastCameraPosition = currentPosition;
		}

		void AdjustParalaxPositionsForArray(GameObject[] layerArray, float speedModifier, float distance)
		{
			for (var index = 0; index < layerArray.Length; index++)
			{
				var currentPosition = layerArray[index].transform.position;
				currentPosition.x = currentPosition.x + distance*speedModifier;
				layerArray[index].transform.position = currentPosition;
			}
		}
	}
}
