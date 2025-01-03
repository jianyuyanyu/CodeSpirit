namespace CodeSpirit.Shared.Entities
{
    [Serializable]
    public abstract class Entity : IEntity
    {
        protected Entity()
        {
        }
    }

    /// <inheritdoc cref="IEntity{TKey}" />
    [Serializable]
    public abstract class Entity<TKey> : Entity, IEntity<TKey>
    {
        /// <inheritdoc/>
        public virtual TKey Id { get; set; }

        protected Entity()
        {

        }
    }
}
