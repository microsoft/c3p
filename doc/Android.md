## Android API projections
Plugin APIs for Android are projected via Java class libraries. Of course, the APIs implemented in Java may also use JNI to call into custom C/C++ code.

### Property getters and setters
Static and instance getter and setter methods are automatically projected as properties.

### Exceptions
Constructors and methods may throw checked or unchecked exceptions. (Property getters and setters may throw
exceptions, though generally that is not a good practice.) Exceptions are propagated out to the caller.

### Async methods
Methods that return `java.util.concurrent.Future<V>` (or a class that implements that `Future` interface) are
automatically projected as async methods. (The C# projection returns a `Task<V>`; the JavaScript projection
returns a `Promise<V>`.) Names of async methods should end with "Async". Async methods that don't return a
value should use a return type of `Future<java.lang.Void>`.

Async methods that fail may throw an exception synchronously or may cause the returned future to throw
an exception asynchronously.

### Events
Given an event named *`EventName`* of type *`EventType`*, the following requirements must be met for the event
to be projected to C# and JavaScript:

 * The event interface must be named `EventNameListener` and must extend `java.util.EventListener`.
 * The event interface must have a single method (of any name) that takes two parameters
   `(Object sender, EventType e)` and returns `void`.
 * There must be methods on the source object named `addEventNameListener` and `removeEventNameListener`
   that each take a single `EventNameListener` parameter.
 * The `EventType` class must extend `java.util.EventObject`, must have no public constructors, only property
   getters, and no other public methods.

### Enums
Android-style integer enum constants can be projected as typed enums. Enum values must be `public static final int`
and must be named with SHOUTY_CASE, with the enum name as the value prefix. For example the following fields
define an enum named "MyEnum" with enumrated values "Zero", "One", and "Two":

    public static final int MY_ENUM_ZERO = 0;
    public static final int MY_ENUM_ONE = 1;
    public static final int MY_ENUM_TWO = 2;

The fields may be included in another class or may be in their own separate class. The enum value names are converted
to the appropriate casing in the projected APIs. In order for the projected APIs to use strongly-typed enums where
appropriate, a `<type-binding>` element is required in plugin.xml for each API where the enum is used. Additionally,
the enum name must be tagged with an `<enum>` element under the `<platform name="android">` element in plugin.xml.

Support for Java strongly-typed enums is in development.

### Collections
Use of Java arrays, `java.util.List<T>`, and `java.util.Map<T>` in Android APIs is projected as corresponding
generic collection types. Other collections types might not be supported.

### URIs, UUIDs, and Dates
The `android.net.Uri`, `java.util.uuid`, and `java.util.Date` classes are specifically supported with appropriate
conversions to corresponding projected types.
