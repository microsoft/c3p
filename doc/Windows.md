## Windows API projections
Plugin APIs for Windows are projected via WinRT. WinRT components are commonly developed using either C# or C++/CX. See [Creating Windows Runtime Components in C# (MSDN)](https://msdn.microsoft.com/en-us/library/br230301.aspx) or [Creating Windows Runtime Components in C++ (MSDN)](https://msdn.microsoft.com/en-us/library/windows/apps/hh441569.aspx).

### Async methods
Async methods on a WinRT component return an `IAsyncAction<V>` or `IAsyncOperation<V>`. These are projected in plugin APIs: the Xamarin C# projection returns a `Task<V>`; the JavaScript projection
returns a `Promise<V>`. Names of async methods should end with an "Async" suffix.

### Events
Standard WinRT events are supported with no special requirements.

### Collections
C# `System.Collections.Generic.IList<T>` and `System.Collections.Generic.IDictionary<K,V>` (and C++/CX equivalents `Windows::Foundation::Collections::IVector<T>` and `Windows::Foundation::Collections::IMap<K,V>) are supported. Other collection types might not be supported.

### URIs, Guids, and Dates
For C#, the `System.Uri`, `System.Guid`, and `System.DateTimeOffset` classes are supported for API projections. The `System.DateTime` class must not be used in plugin APIs; use `System.DateTimeOffset` instead. Because `Guid` and `DateTimeOffset` are value types, they must always be wrapped in `Nullable<T>` as `Guid?` and `DateTimeOffset?`.

For C++, the `Windows::Foundation::Uri`, `Platform::Guid`, and `Windows::Foundation::DateTime` classes are supported for API projections. Because `Platform::Guid` and `Windows::Foundation::DateTime` are value types, they must always be wrapped in `Platform::IBox<T>` as `IBox<Guid>^` and `IBox<DateTime>^`.
