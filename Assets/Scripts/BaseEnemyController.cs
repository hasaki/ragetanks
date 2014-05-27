using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public abstract class BaseEnemyController : MonoBehaviour
{
	public static event Action<int> EnemyDied;

	public int score;

	public BaseEnemyController()
	{
		score = 25;
	}

	protected void OnEnemyDied()
	{
		if (EnemyDied != null)
			EnemyDied(score);
	}
}
