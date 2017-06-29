﻿using System;
using System.Reflection;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store. </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <typeparam name="TGuild">The Guild configuration type.</typeparam>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class EFConfigStore<TContext, TGuild, TChannel, TUser> : IConfigStore<TContext>
        where TContext : EFBaseConfigContext<TGuild, TChannel, TUser>, new()
        where TGuild   : ConfigGuild<TChannel, TUser>, new()
        where TChannel : ConfigChannel<TUser>, new()
        where TUser    : ConfigUser, new()
    {
        /// <summary> Initializes a new instance of <see cref="EFConfigStore{TContext}"/>. </summary>
        /// <param name="parameters">A list of parameters that should be
        /// passed into the construcotr of your <see cref="TContext"/> type. </param>
        public EFConfigStore()
        {
        }

        /// <summary> Loads an instance of the DB Context. </summary>
        public TContext Load()
        {
            return new TContext();
        }

        ///// <summary> Save changes of the DB Context to disk. </summary>
        //public void Save()
        //{
        //    _db.SaveChanges();
        //}
    }



    ///// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    ///// using the default Context type. </summary>
    ///// <typeparam name="TGuild">The Guild configuration type.</typeparam>
    ///// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    ///// <typeparam name="TUser">The User configuration type.</typeparam>
    //public class EFConfigStore<TGuild, TChannel, TUser> : EFConfigStore<EFBaseConfigContext<TGuild, TChannel, TUser>, TGuild, TChannel, TUser>
    //    where TGuild   : ConfigGuild<TChannel, TUser>, new()
    //    where TChannel : ConfigChannel<TUser>, new()
    //    where TUser    : ConfigUser, new()
    //{
    //    public EFConfigStore(EFBaseConfigContext<TGuild, TChannel, TUser> db) : base(db)
    //    {
    //    }
    //}

    ///// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    ///// using the default Context and Guild types. </summary>
    ///// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    ///// <typeparam name="TUser">The User configuration type.</typeparam>
    //public class EFConfigStore<TChannel, TUser> : EFConfigStore<EFConfigBaseContext<TChannel, TUser>, ConfigGuild<TChannel, TUser>, TChannel, TUser>
    //    where TChannel : ConfigChannel<TUser>, new()
    //    where TUser    : ConfigUser, new()
    //{
    //    public EFConfigStore(EFConfigBaseContext<TChannel, TUser> db) : base(db)
    //    {
    //    }
    //}

    ///// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    ///// using the default Context, Guild, and Channel types. </summary>
    ///// <typeparam name="TUser">The User configuration type.</typeparam>
    //public class EFConfigStore<TUser> : EFConfigStore<EFConfigBaseContext<TUser>, ConfigGuild<TUser>, ConfigChannel<TUser>, TUser>
    //    where TUser : ConfigUser, new()
    //{
    //    public EFConfigStore(EFConfigBaseContext<TUser> db) : base(db)
    //    {
    //    }
    //}

    //public class EFConfigStore<TContext> : EFConfigStore<TContext, ConfigGuild, ConfigChannel, ConfigUser>
    //    where TContext : EFBaseConfigContext<ConfigGuild, ConfigChannel, ConfigUser>
    //{
    //    public EFConfigStore(TContext db) : base(db)
    //    {
    //    }
    //}

    ///// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    ///// using the default Context, Guild, Channel, and User types. </summary>
    //public class EFConfigStore : EFConfigStore<EFConfigBaseContext, ConfigGuild, ConfigChannel, ConfigUser>
    //{
    //    public EFConfigStore(EFConfigBaseContext db) : base(db)
    //    {
    //    }
    //}
}
