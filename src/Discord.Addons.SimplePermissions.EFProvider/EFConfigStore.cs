using System;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store.
    /// </summary>
    /// <typeparam name="TContext">The database context.</typeparam>
    public class EFConfigStore<TContext> : IConfigStore<EFConfigBase>
        where TContext : EFConfigBase
    {
        private readonly TContext _db;

        /// <summary>
        /// Initializes a new instance of <see cref="EFConfigStore{TContext}"/>.
        /// </summary>
        /// <param name="db">A function that produces an instance of a DB Context.</param>
        public EFConfigStore(TContext db)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));

            _db = db;
        }

        /// <summary>
        /// Loads an instance of the DB Context.
        /// </summary>
        public EFConfigBase Load()
        {
            return _db;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
