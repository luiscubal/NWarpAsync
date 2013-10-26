NWarpAsync
==========

NWarpAsync is a project to use C#'s async/await to do things that it wasn't... entirely made for.

Async and Await are two keywords introduced in C# 5 that can suspend a function and resume it later.
It was originally meant to be used to simplify the implementation of asynchronous methods but it's far more than that.
Async/Await are general-purpose mechanisms to implement coroutines.

Pretty much *anything* that would benefit from being suspended and later resumed can be simplified with await/async.

This project is licensed under the MIT/Expat license.

NWarpAsync.Yield
----------------

NWarpAsync.Yield is a proof of concept that shows how to implement C# yield return using async/await.
Yield return is an older (and much more restricted) coroutine mechanism in C# that is meant to *generate* enumerations.

NWarpAsync.Yield even handles the disposal of resources and exceptions.

The main class is `Yielder<T>`, which is constructed with a `Func<YieldSink<T>, Task>` instance and is based on `IEnumerable<T>`.
As such, it can be used with foreach loops and even LINQ.

### Example

```csharp
using System.Linq;
using NWarpAsync.Yield;

class Program
{
	static void Main()
	{
		foreach (int value in new Yielder(async yieldSink => {
			await yieldSink.Yield(1);
			await yieldSink.YieldAll(Enumerable.Range(2, 3));
			await yieldSink.Yield(5);
		}))
		{
			//Prints 1 2 3 4 5 (each number in a different line)
			Console.WriteLine(value);
		}
	}
}
```

### Lazy evaluation

The code in the generator function is only executed as needed (when `IEnumerator<T>.MoveNext()` is used).
This is the same as the behavior of the native `yield`.

```csharp
using NWarpAsync.Yield;

class Program
{
	static void Main()
	{
		var yielder = new Yielder(async yieldSink => {
			Console.WriteLine("Foo");
		});
		foreach (int value in yielder) {}
		foreach (int value in yielder) {}
		//Foo is printed twice
		
		var yielder2 = new Yielder(async yieldSink => {
			Console.WriteLine("Bar");
		});
		yielder2.GetEnumerator();
		//yielder2.GetEnumerator().MoveNext() is never used, so the code in it is never executed.
	}
}
```

### Yield break

To implement `yield break;`, just use `return;`. The `Yielder` class will do the rest for you.