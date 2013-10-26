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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NWarpAsync.Yield.Test
{
	[TestFixture]
	public class TestCommon
	{
		[Test]
		public void TestNullArgument ()
		{
			Assert.Throws<ArgumentNullException> (() => new Yielder<object> (null));
		}

		[Test]
		public void TestEmpty ()
		{
			Assert.AreEqual (0, new Yielder<object> (async yieldSink => {
			}).Count ());
		}

		[Test]
		public void TestSingle ()
		{
			var yielder = new Yielder<int> (async yieldSink => await yieldSink.Yield (1));
			Assert.AreEqual (1, yielder.Single ());
		}

		[Test]
		public void TestSimple ()
		{
			int previousValue = 0;
			foreach (int value in new Yielder<int>(async yieldSink =>
            {
                await yieldSink.Yield(1);
                await yieldSink.Yield(2);
                await yieldSink.Yield(3);
            })) {
				Assert.AreEqual (++previousValue, value);
			}

			Assert.AreEqual (3, previousValue);
		}

		[Test]
		public void TestLazy ()
		{
			int value = 0;
			var yielder = new Yielder<int> (async yieldSink => {
				value = 1;
				await yieldSink.Yield (123);
				value = 2;
			});
			Assert.AreEqual (0, value);

			foreach (int collectionValue in yielder) {
				Assert.AreEqual (123, collectionValue);
				Assert.AreEqual (1, value);
			}
			Assert.AreEqual (2, value);
		}
		#pragma warning disable 1998
		#pragma warning disable 4014

		[Test]
		public void TestMissingAwait ()
		{
			var yielder = new Yielder<int> (async yieldSink => {
				yieldSink.Yield (1);
				yieldSink.Yield (2);
			});
			try {
				yielder.GetEnumerator ().MoveNext ();

				Assert.Fail ();
			} catch (AggregateException e) {
				Assert.IsInstanceOf<InvalidOperationException> (e.InnerExceptions.Single ());
			} catch (Exception e) {
				Assert.Fail ();
			}
		}

		[Test]
		public void TestCombinedAwait ()
		{
			var yielder = new Yielder<int> (async yieldSink => {
				yieldSink.Yield (1);
				await yieldSink.Yield (2);
			});
			try {
				yielder.GetEnumerator ().MoveNext ();

				Assert.Fail ();
			} catch (AggregateException e) {
				Assert.IsInstanceOf<InvalidOperationException> (e.InnerExceptions.Single ());
			} catch (Exception e) {
				Assert.Fail ();
			}
		}

		[Test]
		public void TestThreading ()
		{
			var thread1 = new Thread (() => {
				var enumerator = new Yielder<int> (async yieldBuilder => {
					Assert.AreEqual ("Thread 1", Thread.CurrentThread.Name);
					await yieldBuilder.Yield (1);
					Assert.AreEqual ("Thread 2", Thread.CurrentThread.Name);
				}).GetEnumerator ();

				Thread.CurrentThread.Name = "Thread 1";
				enumerator.MoveNext ();

				var thread2 = new Thread (() => {
					Thread.CurrentThread.Name = "Thread 2";
					enumerator.MoveNext ();
				});
				thread2.Start ();
				thread2.Join ();

			});
			thread1.Start ();
			thread1.Join ();
		}

		IEnumerable<int> HelperYielder ()
		{
			yield return 1;
			yield return 2;
			yield return 3;
		}

		[Test]
		public void TestMultiple ()
		{
			var yielder = new Yielder<int> (async yieldSink => {
				await yieldSink.YieldAll (HelperYielder ());
			});
			int previousValue = 0;
			foreach (var value in yielder) {
				Assert.AreEqual (++previousValue, value);
			}
			Assert.AreEqual (3, previousValue);
		}
	}
}
