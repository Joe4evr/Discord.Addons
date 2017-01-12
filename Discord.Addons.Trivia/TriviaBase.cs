using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.TriviaGames
{
    /// <summary> Base class for Trivia games on Discord. </summary>
    public abstract class TriviaBase : ModuleBase
    {
        /// <summary> Indicates if a game is already being played. </summary>
        protected readonly bool GameInProgress;

        /// <summary> The instance of the game being played, if so. </summary>
        protected readonly TriviaGame Game;

        /// <summary> The instance of the <see cref="TriviaService"/>. </summary>
        protected readonly TriviaService Service;

        /// <summary> </summary>
        /// <param name="service"></param>
        protected TriviaBase(TriviaService service)
        {
            GameInProgress = service.TriviaGames.TryGetValue(Context.Channel.Id, out Game);
            Service = service;
        }

        /// <summary> Creates a new Trivia game. </summary>
        /// <param name="turns">Amount of turns the game should last.</param>
        public abstract Task NewGameCmd(int turns);

        /// <summary> End an ongoing Trivia game early. </summary>
        public abstract Task EndGameCmd();
    }
}
