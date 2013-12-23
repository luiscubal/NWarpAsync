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

namespace NWarpAsync.EmulateAwait
{
    public delegate IEnumerable<Tuple<Task, T>> GeneratorFunc<T>(object[] arguments);

    public delegate Task<T> AsyncFunc<T>(params object[] arguments);

    public static class AsyncBuilder
    {
        public static AsyncFunc<T> FromGenerator<T>(GeneratorFunc<T> generator)
        {
            if (generator == null)
            {
                throw new ArgumentNullException("generator");
            }
            return arguments => RunAsAsync<T>(arguments, generator);
        }

        public static async Task<T> RunAsAsync<T>(object[] arguments, GeneratorFunc<T> generator)
        {
            if (generator == null)
            {
                throw new ArgumentNullException("generator");
            }

            foreach (var partialResult in generator(arguments))
            {
                var task = partialResult.Item1;
                if (task == null)
                {
                    return partialResult.Item2;
                }
                await task;
            }

            throw new IncompleteOperationException("Generator function did not return a value");
        }
    }
}
