namespace ZenithAPI
{
	/// <summary>
	/// Entry in the config cache.
	/// </summary>
	/// <typeparam name="T">The type of the value</typeparam>
	public sealed class CacheEntry<T>
	{
		/// <summary>
		/// The value stored in the cache.
		/// </summary>
		public T Value { get; }

		/// <summary>
		/// The expiration time.
		/// </summary>
		public DateTime ExpiresAt { get; }

		/// <summary>
		/// The time when this entry was created.
		/// </summary>
		public DateTime CreatedAt { get; } = DateTime.UtcNow;

		/// <summary>
		/// Whether this entry is expired.
		/// </summary>
		public bool IsExpired => DateTime.UtcNow > ExpiresAt;

		/// <summary>
		/// Creates a new cache entry.
		/// </summary>
		/// <param name="value">The value to store</param>
		/// <param name="expiration">How long the entry is valid for</param>
		public CacheEntry(T value, TimeSpan expiration)
		{
			Value = value;
			ExpiresAt = DateTime.UtcNow.Add(expiration);
		}
	}
}