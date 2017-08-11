using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of <see cref="IPermissionConfig"/> using
    /// in-memory collection as a backing store, suitable for
    /// serialization to and from JSON. </summary>
    public partial class JsonConfigBase : ISetPath
    {
        public CommandService Commands { private get; set; }

        /// <summary> Gets whether fancy help messages are
        /// enabled in a specified guild. </summary>
        public Dictionary<ulong, bool> UseFancyHelps { get; set; }

        /// <summary> Gets the ID of the group that is considered
        /// the Admin role in a specified guild. </summary>
        public Dictionary<ulong, ulong> GuildAdminRole { get; set; }

        /// <summary> Gets the ID of the group that is considered
        /// the Moderator role in a specified guild. </summary>
        public Dictionary<ulong, ulong> GuildModRole { get; set; }

        /// <summary> Gets the list of modules that are
        /// whitelisted in a specified channel. </summary>
        public Dictionary<ulong, HashSet<string>> ChannelModuleWhitelist { get; set; }

        /// <summary> Gets the list of modules that are
        /// whitelisted in a specified guild. </summary>
        public Dictionary<ulong, HashSet<string>> GuildModuleWhitelist { get; set; }

        /// <summary> Gets the users that are allowed to use
        /// commands marked <see cref="MinimumPermission.Special"/>
        /// in a channel. </summary>
        public Dictionary<ulong, HashSet<ulong>> SpecialPermissionUsersList { get; set; }

        public Dictionary<ulong, bool> HidePermCommandValues { get; set; }

        private string _path;

        void ISetPath.SetPath(string path)
        {
            _path = path;
        }

        public void Save()
            => File.WriteAllText(_path, JsonConvert.SerializeObject(this, Formatting.Indented));

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    //File.WriteAllText(_path, JsonConvert.SerializeObject(this, Formatting.Indented));
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~JsonConfigBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    internal interface ISetPath
    {
        void SetPath(string path);
    }
}
