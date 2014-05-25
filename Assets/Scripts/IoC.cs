﻿using Assets.Scripts.Shims;
using Commands;
using TinyIoC;
using UnityEngine;
using System.Collections;

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

	public static void EnsureContainerCreated()
	{
		// ReSharper disable once UnusedVariable
		var container = _container.Value;
	}
}