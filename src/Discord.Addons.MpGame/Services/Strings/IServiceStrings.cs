using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons.MpGame
{
    public interface IServiceStrings
    {
        //MpGameService.ctor()
        string LogRegistration(string gameName);

        //MpGameService.OnGameEnd()
        string CleaningGameData(string gameName, IMessageChannel channel);
        string CleaningDMChannel(IDMChannel channel);
        string CleaningGameString(IMessageChannel channel);

        //MpGameService.OpenNewGame()
        string CreatingGame(string gameName, IMessageChannel channel);

        //MpGameService.RemoveUser()
        string PlayerKicked(IUser user);

        //MpGameService.TryAddNewGame()
        string SettingGame(string gameName, IMessageChannel channel);

        //Player.cs
        string DMsDisabledMessage(IUser user);
        string DMsDisabledKickMessage(IUser user);
    }
}
