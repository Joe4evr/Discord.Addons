## Discord.Addons.Preconditions
Useful preconditions for Discord.Net 1.0

### Example: Ratelimit
Limit how often a user is allowed to invoke a certain command in a given period of time.
```cs
[Command("foo"), Ratelimit(5, 30, Measure.Minutes)]
public async Task Foo()
{
	//....
}
```
