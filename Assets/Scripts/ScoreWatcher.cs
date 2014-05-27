using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
	public class ScoreWatcher : MonoBehaviour
	{
		public PlayerController score;
		private GUIText scoreMesh;

		private int _lastScore = 0;

		void Start()
		{
			scoreMesh = gameObject.GetComponent<GUIText>();
			UpdateScore();
		}

		void LateUpdate()
		{
			if (score != null && score.Score != _lastScore)
				UpdateScore();
		}

		void UpdateScore()
		{
			_lastScore = score.Score;
			scoreMesh.text = string.Format(CultureInfo.CurrentUICulture, "{0:N0}", _lastScore);
		}
	}
}
