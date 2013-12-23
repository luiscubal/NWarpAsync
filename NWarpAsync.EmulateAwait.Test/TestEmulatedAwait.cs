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
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NWarpAsync.EmulateAwait.Test
{
    [TestFixture]
    public class TestEmulatedAwait
    {
        IEnumerable<Tuple<Task, int>> DoNothing(AsyncContext<int> context)
        {
            yield return context.Return(42);
        }

        [Test]
        public void TestEmpty()
        {
            Task<int> task = AsyncBuilder.FromGenerator<int>(DoNothing)();
            task.Wait();
            Assert.AreEqual(42, task.Result);
        }

        [Test]
        public void TestNullArgument()
        {
            Assert.Throws<ArgumentNullException>(() => AsyncBuilder.FromGenerator(default(GeneratorFunc<int>))());
        }

        IEnumerable<Tuple<Task, int>> Incomplete(AsyncContext<int> context)
        {
            yield break;
        }


        [Test]
        public void TestIncompleteMethod()
        {
            try
            {
                AsyncBuilder.FromGenerator<int>(Incomplete)().Wait();
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                Assert.IsInstanceOf<IncompleteOperationException>(e.InnerException);
            }
        }

        IEnumerable<Tuple<Task, bool>> BasicAwait(AsyncContext<bool> context)
        {
            bool done = false;
            yield return context.Await(Task.Factory.StartNew(() => true));
            done = context.GrabLastValue();
            yield return Tuple.Create(default(Task), done);
        }

        [Test]
        public void TestSimpleAwait()
        {
            var task = AsyncBuilder.FromGenerator<bool>(BasicAwait)();
            task.Wait();
            Assert.IsTrue(task.Result);
        }

        IEnumerable<Tuple<Task, bool>> AwaitException(AsyncContext<bool> context)
        {
            bool done = false;
            yield return context.Await(Task.Factory.StartNew(() => { throw new InvalidOperationException(); }));
            try
            {
                context.GrabLastValue();
            }
            catch (AggregateException e) {
                done = e.InnerException is InvalidOperationException;
            }

            yield return context.Return(done);
        }

        [Test]
        public void TestAwaitException()
        {
            var task = AsyncBuilder.FromGenerator<bool>(AwaitException)();
            task.Wait();
            Assert.IsTrue(task.Result);
        }

        IEnumerable<Tuple<Task, int>> MultipleSteps(AsyncContext<int> context)
        {
            yield return context.Await(Task.Factory.StartNew(() => { throw new InvalidOperationException(); }));
            try
            {
                context.GrabLastValue();
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                Assert.IsInstanceOf<InvalidOperationException>(e.InnerException);
            }

            yield return context.Await(Task.Factory.StartNew(() => 42));
            yield return context.Await(Task.Factory.StartNew(() => context.GrabLastValue() * 2));
            yield return context.Return(context.GrabLastValue());
        }

        [Test]
        public void TestMultipleSteps()
        {
            var task = AsyncBuilder.FromGenerator<int>(MultipleSteps)();
            task.Wait();
            Assert.AreEqual(84, task.Result);
        }
    }
}
