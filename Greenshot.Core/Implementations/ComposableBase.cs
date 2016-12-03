using System.Collections.Generic;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core.Implementations
{
	/// <summary>
	/// A simple implementation which takes care of collecting items
	/// </summary>
	/// <typeparam name="TComposable"></typeparam>
	public class ComposableBase<TComposable> : IAmComposable<TComposable>
	{
		private readonly List<TComposable> _composables = new List<TComposable>();

		public IEnumerable<TComposable> Items => _composables.AsReadOnly();

		public IAmComposable<TComposable> Add(TComposable item)
		{
			_composables.Add(item);
			return this;
		}
	}
}
