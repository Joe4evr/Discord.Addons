using System;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store. </summary>
    /// <typeparam name="TContext">The database context.</typeparam>
    public class EFConfigStore<TContext> : IConfigStore<TContext>
        where TContext : EFConfigBase
    {
        private readonly TContext _db;

        /// <summary> Initializes a new instance of <see cref="EFConfigStore{TContext}"/>. </summary>
        /// <param name="db">An instance of a DB Context.</param>
        public EFConfigStore(TContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary> Loads an instance of the DB Context. </summary>
        public TContext Load()
        {
            return _db;
        }

        /// <summary> Save changes of the DB Context to disk. </summary>
        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
