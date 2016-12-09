using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Discord.Addons.TriviaGames
{
    /// <summary>
    /// Creates a Trivia game in a given channel.
    /// </summary>
    public sealed class TriviaGame
    {
        private readonly Stack<QA> _triviaData;
        private readonly IMessageChannel _channel;
        private readonly int _turns;
        private readonly Timer _questionTimer;

        private readonly ConcurrentDictionary<ulong, int> _scoreboard = new ConcurrentDictionary<ulong, int>();
        private readonly HashSet<string> _asked = new HashSet<string>();
        private readonly Random _rng = new Random();

        private QA _currentQuestion;
        private int _turn = 0;
        private Atomic<bool> _isAnswered = new Atomic<bool>(true);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="triviaData"></param>
        /// <param name="channel"></param>
        /// <param name="turns"></param>
        public TriviaGame(IReadOnlyDictionary<string, string[]> triviaData, IMessageChannel channel, int turns)
        {
            _triviaData = new Stack<QA>(triviaData.Select(kv => new QA(kv.Key, kv.Value)).Shuffle(28));
            _channel = channel;
            _turns = turns;

            _questionTimer = new Timer(async obj =>
            {
                await _channel.SendMessageAsync("Time up.");
                if (_asked.Count == _triviaData.Count)
                {
                    await OutOfQuestions();
                }
                else
                {
                    await _channel.SendMessageAsync($"Next question commencing in 15 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(15));
                    await AskQuestion();
                }
            },
            null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Starts the Trivia game.
        /// </summary>
        public async Task Start()
        {
            await _channel.SendMessageAsync("Starting trivia.");
            await AskQuestion();
        }

        /// <summary>
        /// Ends the Trivia game.
        /// </summary>
        public async Task End()
        {
            var sb = new StringBuilder("Score: ```");
            foreach (var kv in _scoreboard)
            {
                sb.AppendLine($"{(await _channel.GetUserAsync(kv.Key))}: {kv.Value} points.");
            }
            sb.Append("```");
            
            await _channel.SendMessageAsync(sb.ToString());
            await GameEnd(_channel.Id);
        }

        private async Task AskQuestion()
        {
            _currentQuestion = _triviaData.Pop();
            _turn++;
            _asked.Add(_currentQuestion.Question);
            _isAnswered = false;
            await _channel.SendMessageAsync(_currentQuestion.Question);
            _questionTimer.Change(TimeSpan.FromSeconds(20), Timeout.InfiniteTimeSpan);
        }

        private async Task OutOfQuestions()
        {
            var winner = (await _channel.GetUserAsync(_scoreboard.OrderByDescending(kv => kv.Value).First().Key)).Username;
            await _channel.SendMessageAsync($"Out of questions. **{winner}** has the most points.");
            await End();
        }

        internal async Task CheckTrivia(SocketMessage msg)
        {
            if (_currentQuestion.Answers.Contains(msg.Content, StringComparer.OrdinalIgnoreCase) &&
                _isAnswered.TryUpdate(newValue: true, comparisonValue: false))
            {
                _questionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _scoreboard.AddOrUpdate(msg.Author.Id, 1, (k, v) => ++v);
                var userScore = _scoreboard.Single(kv => kv.Key == msg.Author.Id).Value;
                await _channel.SendMessageAsync($"Correct. **{msg.Author.Username}** is now at **{userScore}** point(s).");
                if (_turn == _turns)
                {
                    await End();
                }
                else if (_asked.Count == _triviaData.Count)
                {
                    await OutOfQuestions();
                }
                else
                {
                    await _channel.SendMessageAsync("Next question commencing in 15 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(15));
                    await AskQuestion();
                }
            }
        }

        internal event Func<ulong, Task> GameEnd;

        private sealed class QA
        {
            public string Question { get; }
            public string[] Answers { get; }
            public QA(string q, string[] a)
            {
                Question = q;
                Answers = a;
            }
        }

        private sealed class Atomic<T>
            where T : struct
        {
            private readonly object lockObj = new object();
            private T value;

            public Atomic(T initialValue)
            {
                value = initialValue;
            }

            public bool TryUpdate(T newValue, T comparisonValue)
            {
                lock (lockObj)
                {
                    var result = value.Equals(comparisonValue);
                    if (result)
                        value = newValue;

                    return result;
                }
            }

            public static implicit operator T(Atomic<T> atomic) => atomic.value;
            public static implicit operator Atomic<T>(T val) => new Atomic<T>(val);
        }
    }

    internal static class Ext
    {
        //Method for randomizing lists using a Fisher-Yates shuffle.
        //Based on http://stackoverflow.com/questions/273313/
        /// <summary>
        /// Perform a Fisher-Yates shuffle on a collection implementing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="source">The list to shuffle.</param>
        /// <param name="iterations">The amount of iterations you wish to perform.</param>
        /// <remarks>Adapted from http://stackoverflow.com/questions/273313/. </remarks>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, int iterations = 1)
        {
            var provider = RandomNumberGenerator.Create();
            var buffer = source.ToList();
            int n = buffer.Count;
            for (int i = 0; i < iterations; i++)
            {
                while (n > 1)
                {
                    byte[] box = new byte[(n / Byte.MaxValue) + 1];
                    int boxSum;
                    do
                    {
                        provider.GetBytes(box);
                        boxSum = box.Sum(b => b);
                    }
                    while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));
                    int k = (boxSum % n);
                    n--;
                    T value = buffer[k];
                    buffer[k] = buffer[n];
                    buffer[n] = value;
                }
            }

            return buffer;
        }
    }
}