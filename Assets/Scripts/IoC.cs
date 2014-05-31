using RageTanks.Shims;
using TinyIoC;
using UnityEngine;

namespace RageTanks
{
	public class IoC : MonoBehaviour
	{
		// ReSharper disable once InconsistentNaming
		private static readonly Lazy<TinyIoC.TinyIoCContainer> _container = new Lazy<TinyIoCContainer>(Initialize);

		private static TinyIoCContainer Initialize()
		{
			var container = TinyIoCContainer.Current;

			container.AutoRegister();

			return container;
		}

		public static TinyIoCContainer Container
		{
			get { return _container.Value; }
		}

		public static TinyIoCContainer EnsureContainerCreated()
		{
			return _container.Value;
		}
	}
}
