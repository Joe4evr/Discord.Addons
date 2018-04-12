using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons.MpGame
{
    public interface ILogStrings
    {
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

        //Player.cs
        string DMsDisabledMessage(IUser user);
        string DMsDisabledKickMessage(IUser user);
    }
}
