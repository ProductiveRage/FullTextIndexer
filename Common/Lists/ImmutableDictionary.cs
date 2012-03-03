using System;
using System.Collections.Generic;

namespace Common.Lists
{
    [Serializable]
    public class ImmutableDictionary<TKey, TValue>
    {
		private Dictionary<TKey, TValue> _data;
		private IEqualityComparer<TKey> _keyComparer;
		public ImmutableDictionary(Dictionary<TKey, TValue> data, IEqualityComparer<TKey> keyComparer)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			if (keyComparer == null)
				throw new ArgumentNullException("keyComparer");

			_data = new Dictionary<TKey, TValue>(data, keyComparer);
			_keyComparer = keyComparer;
		}
		public ImmutableDictionary(Dictionary<TKey, TValue> data) : this(data, new StandardEqualityComparer<TKey>()) { }
		public ImmutableDictionary(IEqualityComparer<TKey> keyComparer) : this(new Dictionary<TKey, TValue>(), keyComparer) { }
		public ImmutableDictionary() : this(new Dictionary<TKey, TValue>(), new StandardEqualityComparer<TKey>()) { }

		/// <summary>
		/// This will raise an exception for a null or invalid key
		/// </summary>
		public TValue this[TKey key]
		{
			get
			{
				if (key == null)
					throw new ArgumentNullException("key");

				if (!_data.ContainsKey(key))
					throw new ArgumentException("Invalid key: " + key.ToString());
				return _data[key];
			}
		}

		/// <summary>
		/// This will throw an exception for null input
		/// </summary>
		public bool ContainsKey(TKey key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return _data.ContainsKey(key);
		}

		/// <summary>
		/// This will never return null nor contain any null values
		/// </summary>
		public ImmutableList<TKey> Keys
		{
			get { return _data.Keys.ToImmutableList(); }
		}

		/// <summary>
		/// This will raise an exception for a null key
		/// </summary>
		public ImmutableDictionary<TKey, TValue> AddOrUpdate(TKey key, TValue value)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			var dataNew = new Dictionary<TKey, TValue>(_data, _keyComparer);
			if (dataNew.ContainsKey(key))
				dataNew[key] = value;
			else
				dataNew.Add(key, value);
			return new ImmutableDictionary<TKey, TValue>()
			{
				_data = dataNew,
				_keyComparer = _keyComparer
			};
		}

		/// <summary>
		/// This will raise an exception for a null key
		/// </summary>
		public ImmutableDictionary<TKey, TValue> RemoveIfPresent(TKey key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			var dataNew = new Dictionary<TKey, TValue>(_data, _keyComparer);
			if (dataNew.ContainsKey(key))
				dataNew.Remove(key);
			return new ImmutableDictionary<TKey, TValue>()
			{
				_data = dataNew,
				_keyComparer = _keyComparer
			};
		}

		/// <summary>
		/// This is just a convenience method so that derived types can call Add, Remove, etc.. and return instances of themselves without having to
		/// pass that data back through a constructor which will check each value against the validator even though we already know they're valid!
		/// Note: This can only be used by derived classes that don't have any new requirements of any type - we're setting only the values and
		/// validator references here!
		/// </summary>
		protected static U toDerivedClass<U>(ImmutableDictionary<TKey, TValue> dictionary) where U : ImmutableDictionary<TKey, TValue>, new()
		{
			if (dictionary == null)
				throw new ArgumentNullException("dictionary");

			// Use same trick as above methods to cheat - we're changing the state of the object after instantiation, but after returning from
			// this method it can be considered immutable
			return new U()
			{
				_data = dictionary._data,
				_keyComparer = dictionary._keyComparer
			};
		}

		private class StandardEqualityComparer<T> : IEqualityComparer<T>
		{
			public bool Equals(T x, T y)
			{
				if (x == null)
					throw new ArgumentNullException("x");
				if (y == null)
					throw new ArgumentNullException("y");
				return x.Equals(y);
			}

			public int GetHashCode(T obj)
			{
				if (obj == null)
					throw new ArgumentNullException("obj");
				return obj.GetHashCode();
			}
		}
	}
}
