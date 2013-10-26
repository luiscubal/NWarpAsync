//Copyright (c) 2013 Luís Reis <luiscubal@gmail.com>
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NWarpAsync.Yield
{
	/// <summary>
	/// YieldSink is the class a generator function should output its yielded values to.
	/// </summary>
	public class YieldSink<T> : IDisposable
	{
		internal Action NextAction;
		internal T Current;
		internal bool HasValue;

		public bool Disposed { get; private set; }

		internal YieldSink ()
		{
		}

		~YieldSink ()
		{
			if (!Disposed) {
				TriggerDispose ();
			}
		}

		public void Dispose ()
		{
			if (!Disposed) {
				TriggerDispose ();

				GC.SuppressFinalize (this);
			}
		}

		void TriggerDispose ()
		{
			Disposed = true;
			if (NextAction != null) {
				NextAction ();
			}
		}

		/// <summary>
		/// Outputs a single value.
		/// The return value of this method should be awaited by the generator function
		///  before returning control or calling Yield again.
		/// </summary>
		/// <param name="value">The value to yield.</param>
		public YieldAwaitable<T> Yield (T value)
		{
			if (HasValue) {
				throw new InvalidOperationException ("Yielded additional value before MoveNext(). This is probably caused by a missing await.");
			}

			Current = value;
			HasValue = true;

			return new YieldAwaitable<T> (this);
		}

		/// <summary>
		/// Outputs multiple values.
		/// </summary>
		/// <param name="enumerable">The values to yield. If null, no values will be yielded.</param>
		/// <returns>A Task that is completed when all items in the enumerable have been yielded.</returns>
		public async Task YieldAll (IEnumerable<T> enumerable)
		{
			foreach (T value in enumerable ?? Enumerable.Empty<T>()) {
				await Yield (value);
			}
		}
	}
}
