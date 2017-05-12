using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;

namespace Scratchpad
{
    public class Service
    {
        private readonly Timer _timer; // 2) Add a field like this

        public Service()
        {
            _timer = new Timer(_ =>
            {
                // 3) Any code you want to periodically run goes here
            },
            null,
            TimeSpan.FromMinutes(10),  // 4) Time that message should fire after bot has started
            TimeSpan.FromMinutes(30)); // 5) Time after which message should repeat (`Timeout.Infinite` for no repeat)
        }

        public void Stop() // 6) Example to make the timer stop running
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Restart() // 7) Example to restart the timer
        {
            _timer.Change(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30));
        }
    }

    public class Module : ModuleBase
    {
        private readonly Service _service;

        public Module(Service service)
        {
            _service = service;
        }

        [Command("stoptimer")]
        public async Task StopCmd()
        {
            _service.Stop();
            await ReplyAsync("Timer stopped.");
        }

        [Command("starttimer")]
        public async Task RestartCmd()
        {
            _service.Restart();
            await ReplyAsync("Timer (re)started.");
        }
    }
}
