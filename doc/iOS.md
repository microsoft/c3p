## iOS API projections
Plugin APIs for iOS must be projected using Objective-C. Swift is not yet supported.

### Property getters and setters
Obj-C has support for instance properties, but not static properties. Static getter and setter methods in Obj-C
are projected as static properties.

### Exceptions
A synchronous initializer or method may report a failure if the last parameter is an out `NSError` parameter.
If the error is set to a non-null value upon return, then the error gets projected as a thrown exception.

Initializers or methods with out-error parameters must use one of the following selector naming patterns to
enable correct resolution at runtime:

    initError:
    initWithError:
    initWithOtherParameters:error:
    initWithOtherParameters:withError:
    methodError:
    methodWithError:
    methodWithOtherParameters:error:
    methodWithOtherParameters:withError:

### Async methods
Common pratice for async methods in Obj-C is to use callback parameters. The following patterns are required
to enable projection of async methods from Obj-C:

 * An async method must return void.
 * An async method must have a success completion block as the last parameter, or success and failure completion
   blocks as the last two parameters.
 * The success completion block *may* have a single parameter which is the async result; if not then the method
   asynchronously returns void.
 * A failure completion block, if specified, must have a single `NSError` parameter. The NSError passed to the
   failure completion block gets projected as a thrown exception.
 * An async method must invoke either the success or failure completion block at some point after it is called.
   (The success or failure completion block may be invoked synchronously, if the method finds no need to
   start an async operation.)

Async methods must use one of the following selector naming patterns to enable correct resolution at runtime:

    methodThen:                                  // Success callback
    methodAndThen:                               // Success callback
    methodWithOtherParameters:then:              // Success callback
    methodWithOtherParameters:andThen:           // Success callback
    methodResult:                                // Result callback
    methodWithResult:                            // Result callback
    methodWithOtherParameters:result:            // Result callback
    methodWithOtherParameters:withResult:        // Result callback
    methodThen:catch:                            // Success and failure callbacks
    methodAndThen:catch:                         // Success and failure callbacks
    methodWithOtherParameters:then:catch:        // Success and failure callbacks
    methodWithOtherParameters:andThen:catch:     // Success and failure callbacks
    methodResult:catch:                          // Result and failure callbacks
    methodWithResult:catch:                      // Result and failure callbacks
    methodWithOtherParameters:result:catch:      // Result and failure callbacks
    methodWithOtherParameters:withResult:catch:  // Result and failure callbacks

Note initializers (init methods) *must not* be async.

### Events
The common Obj-C "delegate" pattern is not supported, because it only allows for *unicast* events (a single receiver).
Instead, projection of plugin APIs requires Obj-C events to multicast. Given an event named *`EventName`* of
type *`EventType`*, the following requirements must be met for the event to be projected to C# and JavaScript:

 * An typedef for an event listener callback block must be named `EventNameListener` and must take two parameters
   `(NSObject* sender, EventType e)` and return `void`.
 * There must be methods on the source object named `addEventNameListener` and `removeEventNameListener`
   that each take a single `EventNameListener` (callback block) parameter.
 * The `EventType` class must have no public initializers, only readonly instance properties, and no public methods.

### Enums
Use `NS_ENUM` or `NS_OPTIONS` to define enumerations.

### Number types
Obj-C APIs typically use `NSNumber` to represent a boxed/nullable value of any primitive numeric type. To enable
projections as more specific types, a `<type-binding>` element is required in plugin.xml for any uses of `NSNumber`.

### Collections
Use of `NSArray<T>` and `NSDictionary<T>` in Obj-C APIs is projected as corresponding generic collection types.
Other collections types might not be supported.

### URIs, Guids, and Dates
The `NSURL`, `NSUUID`, and `NSDate` classes are specifically supported with appropriate conversions to
corresponding projected types.
