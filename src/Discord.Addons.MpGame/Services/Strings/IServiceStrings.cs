using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons.MpGame
{
    public interface IServiceStrings
    {
        //ctor
        string LogRegistration(string gameName);

        //OnGameEnd
        string CleaningGameData(string gameName, IMessageChannel channel);
        string CleaningDMChannel(IDMChannel channel);
        string CleaningGameString(IMessageChannel channel);

        //OpenNewGame
        string CreatingGame(string gameName, IMessageChannel channel);

        //RemoveUser
        string PlayerKicked(IUser user);

        //TryAddNewGame
        string SettingGame(string gameName, IMessageChannel channel);

    }
}
