The Config Store
-----------

Once your Cofig Context is in order, you can use it to initialize the Config Store.
An easy way to handle this is to declare a class that fills in the appropriate types.
```cs
public class ExampleConfigStore : EFConfigStore<ExampleConfigContext, GuildType, ChannelType, UserType>
{
    public EFConfigStore(ExampleConfigContext db) : base(db)
    {
    }
}

// ....
var store = new ExampleConfigStore(new ExampleConfigContext());
```

[<- Part 3 - The Config Base Context](3-ConfigBaseContext.md) - The Config Store