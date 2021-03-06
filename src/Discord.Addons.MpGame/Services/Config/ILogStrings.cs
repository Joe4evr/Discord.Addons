﻿using System;

namespace Discord.Addons.MpGame
{
    /// <summary>
    ///     Contract for the log strings used in a <see cref="MpGameService{TGame, TPlayer}"/>.
    /// </summary>
    public partial interface ILogStrings
    {
        //MpGameService.ctor()
        /// <summary>
        ///     Called when the game service is registered.
        /// </summary>
        /// <param name="gameName">
        ///     Name of the gametype for which this service is registered.
        /// </param>
        string LogRegistration(string gameName);

        //MpGameService.OnGameEnd()
        /// <summary>
        /// 
        /// </summary>
        string CleaningGameData(IMessageChannel channel, string gameName);
        /// <summary>
        /// 
        /// </summary>
        string CleaningDMChannel(IDMChannel channel);
        /// <summary>
        /// 
        /// </summary>
        string CleaningGameString(IMessageChannel channel);

        //MpGameService.OpenNewGame()
        /// <summary>
        /// 
        /// </summary>
        string CreatingGame(IMessageChannel channel, string gameName);

        //MpGameService.RemoveUser()
        /// <summary>
        /// 
        /// </summary>
        string PlayerKicked(IUser user);

        //MpGameService.TryAddNewGame()
        /// <summary>
        /// 
        /// </summary>
        string SettingGame(IMessageChannel channel, string gameName);

        //MpGameModuleBase.BeforeExecute()
        /// <summary>
        /// 
        /// </summary>
        string RegisteringPlayerTypeReader(string typeName);

        //Player.cs
        /// <summary>
        /// 
        /// </summary>
        string DMsDisabledMessage(IUser user);
        /// <summary>
        /// 
        /// </summary>
        string DMsDisabledKickMessage(IUser user);
    }
}
