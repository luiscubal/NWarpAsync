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
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

namespace NWarpAsync.Yield
{
	/// <summary>
	/// The yielder is the class that implements yield on top of await.
	/// </summary>
	/// <example>
	/// This example shows how to use the Yielder class.
	/// <code>
	/// using System;
	/// using NWarpAsync.Yield;
	/// 
	/// class MyClass
	/// {
	///    public static void Main ()
	///    {
	///       //Prints 1 2 3 (each in a different line)
	///       foreach (int value in new Yielder(async yieldBuilder => {
	///          await yieldBuilder.Yield(1);
	///          await yieldBuilder.Yield(2);
	///          await yieldBuilder.Yield(3);
	///       }))
	///       {
	///          Console.WriteLine(value);
	///       }
	///    }
	/// }
	/// </code>
	/// </example>
	public class Yielder<T> : IEnumerable<T>
	{
		readonly Func<YieldSink<T>, Task> yieldBuilder;

		/// <summary>
		/// Creates a new Yielder instance using the indicated function to
		///  generate the values.
		/// </summary>
		/// <param name="yieldBuilder">The generator function. Must not be null.</param>
		/// <exception cref="System.ArgumentNullException">If yieldBuilder is null.</exception>
        public Yielder(Func<YieldSink<T>, Task> yieldBuilder)
		{
			if (yieldBuilder == null)
				throw new ArgumentNullException ("yieldBuilder");

			this.yieldBuilder = yieldBuilder;
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator ();
		}

		public IEnumerator<T> GetEnumerator() {
			var yieldSink = new YieldSink<T>();
			return new YieldConsumer(yieldSink, yieldBuilder);
		}

		class YieldConsumer : IEnumerator<T> {
			readonly YieldSink<T> sink;
            readonly Func<YieldSink<T>, Task> builder;
            Task currentTask;
			bool suspended;

            internal YieldConsumer(YieldSink<T> sink, Func<YieldSink<T>, Task> builder)
            {
				this.sink = sink;
				this.builder = builder;
			}

			public bool MoveNext ()
			{
				if (sink.Disposed)
					return false;

				sink.HasValue = false;

				if (suspended) {
					var nextAction = sink.NextAction;
					sink.NextAction = null;
					nextAction ();
				} else {
					currentTask = builder (sink);
					suspended = true;
				}

                if (currentTask == null)
                {
                    throw new InvalidOperationException("builder function returned null");
                }

                if (currentTask.Exception != null)
                {
                    var exceptions = currentTask.Exception.InnerExceptions
                        .Where(innerException => !(innerException is YieldInterruptionException)).ToList();
                    if (exceptions.Count != 0)
                    {
                        throw new AggregateException(exceptions);
                    }
                }

				if (!sink.HasValue) {
					DisposeSink ();
				}

				return sink.HasValue;
			}

			public void Reset ()
			{
				throw new NotImplementedException ();
			}

			object IEnumerator.Current {
				get {
					return Current;
				}
			}

			public void Dispose ()
			{
				DisposeSink ();
			}

			void DisposeSink ()
			{
				try {
					sink.Dispose ();
				} catch (YieldInterruptionException) {
					//Ignore exception here.
					//YieldInterruptionException is only used to interrupt the flow
					// in the builder function.
					//Code outside the builder function should not be aware of the
					// exception.
				}
			}

			public T Current {
				get {
					return sink.Current;
				}
			}
		}
	}
}

