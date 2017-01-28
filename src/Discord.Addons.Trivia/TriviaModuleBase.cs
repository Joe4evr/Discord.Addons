using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.TriviaGames
{
    /// <summary> Base class for Trivia games on Discord. </summary>
    public abstract class TriviaModuleBase : ModuleBase<ICommandContext>
    {
        /// <summary> Indicates if a game is already being played. </summary>
        protected bool GameInProgress;

        /// <summary> The instance of the game being played, if so. </summary>
        protected TriviaGame Game;

        /// <summary> The instance of the <see cref="TriviaService"/>. </summary>
        protected readonly TriviaService Service;

        /// <summary> </summary>
        /// <param name="service"></param>
        protected TriviaModuleBase(TriviaService service)
        {
            Service = service;
        }

        //protected override void BeforeExecute()
        //{
        //    GameInProgress = Service.TriviaGames.TryGetValue(Context.Channel.Id, out Game);
        //}

        /// <summary> Creates a new Trivia game. </summary>
        /// <param name="turns">Amount of turns the game should last.</param>
        public abstract Task NewGameCmd(int turns);

        /// <summary> End an ongoing Trivia game early. </summary>
        public abstract Task EndGameCmd();
    }
}
