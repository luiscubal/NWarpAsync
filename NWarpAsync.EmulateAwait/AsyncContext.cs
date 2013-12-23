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
    public class AsyncContext<T>
    {
        dynamic[] arguments;
        bool hasLastValueReady;
        dynamic lastReturnedValue;
        AggregateException lastException;
        public AsyncContext(dynamic[] arguments)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException("arguments");
            }
            this.arguments = arguments;
        }

        public TaskIteration<T> Return(T value)
        {
            if (hasLastValueReady)
            {
                throw new InvalidOperationException("Returning before calling GrabLastValue() first");
            }
            return new TaskIteration<T>(null, value);
        }

        Type GetGenericTaskType(Task task)
        {
            Type type = task.GetType();
            while (type != typeof(Task) && type.GetGenericTypeDefinition() != typeof(Task<>))
            {
                type = type.BaseType;
            }

            if (type != typeof(Task))
            {
                var genericTypes = type.GetGenericArguments();
                return genericTypes[0];
            }

            return null;
        }

        public TaskIteration<T> Await(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }
            if (hasLastValueReady)
            {
                throw new InvalidOperationException("Awaiting before calling GrabLastValue() first");
            }
            return new TaskIteration<T>(task.ContinueWith(precedent =>
            {
                lastException = precedent.Exception;
                var genericTaskType = GetGenericTaskType(precedent);
                if (genericTaskType != null)
                {
                    dynamic genericTask = precedent;
                    lastReturnedValue = genericTask.Result;
                }
                hasLastValueReady = true;
            }), default(T));
        }

        public dynamic GrabLastValue()
        {
            if (!hasLastValueReady)
            {
                throw new InvalidOperationException("Used GrabLastValue() without awaiting anything first.");
            }
            hasLastValueReady = false;

            if (lastException != null)
            {
                throw lastException;
            }
            return lastReturnedValue;
        }

        public int ArgumentsCount
        {
            get { return arguments.Length; }
        }

        public TResult Argument<TResult>(int position)
        {
            return arguments[position];
        }
    }
}
