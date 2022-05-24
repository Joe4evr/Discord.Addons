#if NET6_0_OR_GREATER
namespace MpGame.Tests.Analyzers;

internal static class SourceTexts
{
    internal const string MockModule = @"using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;

namespace MpGame.Tests.ResourceMocks;

public sealed class MockModule : MpGameModuleBase<MockService, MockGame, Player>
{
    public MockModule(MockService gameService)
        : base(gameService)
    {
    }

    [RequireGameState<MockGameState>(MockGameState.Things)]
    public override Task CancelGameCmd() => throw new NotImplementedException();
    [RequireGameStateOneOf<MockGameState>(MockGameState.Things, MockGameState.Stuff)]
    public override Task EndGameCmd() => throw new NotImplementedException();
    public override Task GameStateCmd() => throw new NotImplementedException();
    public override Task JoinGameCmd() => throw new NotImplementedException();
    public override Task LeaveGameCmd() => throw new NotImplementedException();
    public override Task NextTurnCmd() => throw new NotImplementedException();
    public override Task OpenGameCmd() => throw new NotImplementedException();
    public override Task StartGameCmd() => throw new NotImplementedException();
}
";
    internal const string MockModuleOneOfError = @"using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;

namespace MpGame.Tests.ResourceMocks;

public sealed class MockModule : MpGameModuleBase<MockService, MockGame, Player>
{
    public MockModule(MockService gameService)
        : base(gameService)
    {
    }

    [RequireGameStateOneOf<MockGameState>(MockGameState.Things)]
    public override Task CancelGameCmd() => throw new NotImplementedException();
    [RequireGameStateOneOf<MockGameState>()]
    public override Task EndGameCmd() => throw new NotImplementedException();
    [RequireGameStateOneOf<MockGameState>]
    public override Task GameStateCmd() => throw new NotImplementedException();
    public override Task JoinGameCmd() => throw new NotImplementedException();
    public override Task LeaveGameCmd() => throw new NotImplementedException();
    public override Task NextTurnCmd() => throw new NotImplementedException();
    public override Task OpenGameCmd() => throw new NotImplementedException();
    public override Task StartGameCmd() => throw new NotImplementedException();
}
";

    internal const string MockGameState = @"namespace MpGame.Tests.ResourceMocks;

public enum MockGameState
{
    StartOfGame,
    StartOfTurn,
    Things,
    Stuff,
    EndOfTurn
}

public enum WrongGameState { }
";

    internal const string MockGameNoStateDiag = @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;

namespace MpGame.Tests.ResourceMocks;

public sealed class MockGame : GameBase<Player>
{
    public MockGame(IMessageChannel channel, IEnumerable<Player> players)
        : base(channel, players, false)
    {
    }

    public override Task SetupGame() => throw new NotImplementedException();
    public override Task StartGame() => throw new NotImplementedException();
    public override Task NextTurn() => throw new NotImplementedException();
    public override string GetGameState() => throw new NotImplementedException();
    public override Embed GetGameStateEmbed() => throw new NotImplementedException();
}
";

    internal const string MockGameWrongStateDiag = @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;

namespace MpGame.Tests.ResourceMocks;

public sealed class MockGame : GameBase<Player>, ISimpleStateProvider<WrongGameState>
{
    public MockGame(IMessageChannel channel, IEnumerable<Player> players)
        : base(channel, players, false)
    {
    }

    public WrongGameState State { get; private set; }

    public override Task SetupGame() => throw new NotImplementedException();
    public override Task StartGame() => throw new NotImplementedException();
    public override Task NextTurn() => throw new NotImplementedException();
    public override string GetGameState() => throw new NotImplementedException();
    public override Embed GetGameStateEmbed() => throw new NotImplementedException();
}
";

    internal const string MockGameValid = @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;

namespace MpGame.Tests.ResourceMocks;

public sealed class MockGame : GameBase<Player>, ISimpleStateProvider<MockGameState>
{
    public MockGame(IMessageChannel channel, IEnumerable<Player> players)
        : base(channel, players, false)
    {
    }

    public MockGameState State { get; private set; }

    public override Task SetupGame() => throw new NotImplementedException();
    public override Task StartGame() => throw new NotImplementedException();
    public override Task NextTurn() => throw new NotImplementedException();
    public override string GetGameState() => throw new NotImplementedException();
    public override Embed GetGameStateEmbed() => throw new NotImplementedException();
}
";

    internal const string MockService = @"using System;
using Discord;
using Discord.WebSocket;
using Discord.Addons.MpGame;

namespace MpGame.Tests.ResourceMocks;

public sealed class MockService : MpGameService<MockGame>
{
    public MockService(
        BaseSocketClient client,
        IMpGameServiceConfig? mpconfig = null,
        Func<LogMessage, Task>? logger = null)
        : base(client, mpconfig, logger)
    {
    }
}
";
}
#endif
