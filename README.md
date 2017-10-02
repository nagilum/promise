# Promise

C# Promise library

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