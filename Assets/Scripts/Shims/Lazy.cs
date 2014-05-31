using System;

namespace RageTanks.Shims
{
	public class Lazy<TReturnType>
	{
		private readonly object _lock = new object();
		private readonly Func<TReturnType> _factory; 
		
		private bool _initialized = false;
		private TReturnType _instance;

		public Lazy(Func<TReturnType> instanceFactory)
		{
			if (instanceFactory == null) 
				throw new ArgumentNullException("instanceFactory");
			
			_factory = instanceFactory;
		}

		public TReturnType Value
		{
			get
			{
				if (!_initialized)
				{
					lock (_lock)
					{
						if (!_initialized)
						{
							_instance = _factory();
							_initialized = true;
						}
					}
				}

				return _instance;
			}
		}
	}
}
