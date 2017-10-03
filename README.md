# Promise

C# Promise-like library

Yes it handles promises within promises.

## Async Call

```csharp
var promise = new Promise()
    .Then(GetPersonList)
    .Then<List<Person>, List<int>>(GetPersonsAges)
    .Then<List<int>, int>(CombineAges)
    .Catch<NullReferenceException>(HandleNullReferenceException)
    .Catch(HandleException)
    .ExecuteAsync();

while (promise.IsRunning) {
    Task.Delay(1);
}

var age = promise.CastTo<int>();
```

## Sync Call

```csharp
var age = new Promise()
    .Then(GetPersonList)
    .Then<List<Person>, List<int>>(GetPersonsAges)
    .Then<List<int>, int>(CombineAges)
    .Catch<NullReferenceException>(HandleNullReferenceException)
    .Catch(HandleException)
    .Execute()
    .CastTo<int>();
```

## Use Lambda functions

```csharp
var age = new Promise()
    .Then(() => new Dictionary<string, int> {
        {"Barack Obama", 56},
        {"Albert Einstein", 76}
    })
    .Then((Dictionary<string, int> persons) => {
        return persons.Sum(p => p.Value);
    })
    .Execute()
    .CastTo<int>();
```

## Properties

```csharp
// Indicates whether or not the promise is still executing.
bool IsRunning

// Output of the last function.
object Result
```

## Helper functions

```csharp
// Attempt to cast the Result to given type.
CastTo<T>()

// Wait for the promise to finish executing.
Wait()
```