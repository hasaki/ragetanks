using UnityEngine;

namespace RageTanks.Commands
{
	class StartCommand : MonoBehaviour, ICommand
	{
		void OnEnable()
		{
			Execute();
		}

		public void Execute()
		{
			IoC.EnsureContainerCreated();
			Application.LoadLevel(1);
		}
	}
}
