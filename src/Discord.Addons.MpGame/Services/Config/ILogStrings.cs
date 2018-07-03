using System;

namespace Discord.Addons.MpGame
{
    /// <summary>
    ///     Contract for the log strings used in a <see cref="MpGameService{TGame, TPlayer}"/>.
    /// </summary>
    public /*partial*/ interface ILogStrings
    {
        //public static ILogStrings Default { get; } = new DefaultLogStrings();

        //MpGameService.ctor()
        string LogRegistration(string gameName);

        //MpGameService.OnGameEnd()
        string CleaningGameData(IMessageChannel channel, string gameName);
        string CleaningDMChannel(IDMChannel channel);
        string CleaningGameString(IMessageChannel channel);

        //MpGameService.OpenNewGame()
        string CreatingGame(IMessageChannel channel, string gameName);

        //MpGameService.RemoveUser()
        string PlayerKicked(IUser user);

        //MpGameService.TryAddNewGame()
        string SettingGame(IMessageChannel channel, string gameName);

        //MpGameModuleBase.BeforeExecute()
        string RegisteringPlayerTypeReader(string typeName);

        //Player.cs
        string DMsDisabledMessage(IUser user);
        string DMsDisabledKickMessage(IUser user);
    }
}
