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
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;

namespace NWarpAsync.Yield.Test
{
	[TestFixture]
	public class TestDispose
	{
		class MockDisposable : IDisposable
		{
			public int DisposeCount;

			public void Dispose ()
			{
				DisposeCount++;
			}
		}

		[Test]
		public void TestSimpleConclusion ()
		{
			var disposable = new MockDisposable ();
			foreach (var value in new Yielder<int>(async yieldSink =>
            {
                using (disposable)
                {
                    await yieldSink.Yield(1);
                }
            })) {
				Assert.AreEqual (1, value);
				Assert.AreEqual (0, disposable.DisposeCount);
			}

			Assert.AreEqual (1, disposable.DisposeCount);
		}

		static IEnumerable<int> HelperMethod (IDisposable disposable)
		{
			return new Yielder<int> (async yieldSink => {
				using (disposable) {
					await yieldSink.Yield (1);
					await yieldSink.Yield (2);
					await yieldSink.Yield (3);
				}
			});
		}

		[Test]
		public void TestForcedConclusion ()
		{
			var disposable = new MockDisposable ();
			foreach (var val in HelperMethod(disposable).Take(1)) {
				Assert.AreEqual (1, val);
			}
			Assert.AreEqual (1, disposable.DisposeCount);
		}
	}
}
