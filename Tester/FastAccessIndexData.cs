using System;
using System.Collections.Generic;

namespace Tester
{
    /// <summary>
    /// This didn't seem to perform significantly better than a Dictionary :(
    /// </summary>
    [Serializable]
    public class FastAccessIndexData<TKey, TValue>
    {
        private Node _topNode;
        private IEqualityComparer<TKey> _keyComparer;
        private uint _groupSize;
        public FastAccessIndexData(IEnumerable<KeyValuePair<TKey, TValue>> data, IEqualityComparer<TKey> keyComparer, uint groupSize)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (keyComparer == null)
                throw new ArgumentNullException("keyComparer");
            if (groupSize < 2)
                throw new ArgumentOutOfRangeException("groupSize", "must be two or greater");

            var topNode = new Node(groupSize, keyComparer);
            var keys = new HashSet<TKey>(keyComparer);
            foreach (var entry in data)
            {
                if (entry.Key == null)
                    throw new ArgumentException("Null key encountered in data");
                if (keys.Contains(entry.Key))
                    throw new ArgumentException("Duplicate key encountered in data");

                var valueContainer = TryToGetValueContainer(entry.Key, keyComparer, topNode, groupSize, true);
                valueContainer.Add(entry.Key, entry.Value);
            }

            _topNode = topNode;
            _keyComparer = keyComparer;
            _groupSize = groupSize;
        }

        /// <summary>
        /// This will throw an exception for a null key or if the key is not present in the data
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException("key");

                var resultData = TryToGet(key);
                if (!resultData.IsKeyPresent)
                    throw new ArgumentException("key is not present in data");
                return resultData.Value;
            }
        }

        /// <summary>
        /// This will throw an exception for a null key. The value output parameter should only be considered defined if this method returns true.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var resultData = TryToGet(key);
            value = resultData.Value;
            return resultData.IsKeyPresent;
        }

        private LookupResult TryToGet(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var valueContainer = TryToGetValueContainer(key, _keyComparer, _topNode, _groupSize, false);
            if (valueContainer == null)
                return new LookupResult(false, default(TValue));
            
            foreach (var value in valueContainer)
            {
                if (_keyComparer.Equals(value.Key, key))
                    return new LookupResult(true, value.Value);
            }
            return new LookupResult(false, default(TValue));
        }

        private static Dictionary<TKey, TValue> TryToGetValueContainer(TKey key, IEqualityComparer<TKey> keyComparer, Node topNode, uint groupSize, bool createIfNotPresent)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (keyComparer == null)
                throw new ArgumentNullException("keyComparer");
            if (topNode == null)
                throw new ArgumentNullException("topNode");

            var node = topNode;
            var hash = GetUnsignedHashCode(key, keyComparer);
            while (hash > 1)
            {
                var segment = hash % groupSize;
                if (node.ChildNodes[segment] == null)
                {
                    if (!createIfNotPresent)
                        return null;

                    node.ChildNodes[segment] = new Node(groupSize, keyComparer);
                }
                node = node.ChildNodes[segment];
                hash = hash / groupSize;
            }
            return node.Values;
        }

        private static uint GetUnsignedHashCode(TKey key, IEqualityComparer<TKey> keyComparer)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (keyComparer == null)
                throw new ArgumentNullException("keyComparer");

            var hash = keyComparer.GetHashCode(key);
            if (hash >= 0)
                return (uint)hash;
            return (uint)(int.MaxValue) + (uint)(-hash);
        }

        [Serializable]
        private class Node
        {
            public Node(uint childNodeCount, IEqualityComparer<TKey> keyComparer)
            {
                if (childNodeCount < 2)
                    throw new ArgumentOutOfRangeException("childNodeCount", "must be two or greater");
                if (keyComparer == null)
                    throw new ArgumentNullException("keyComparer");
                
                ChildNodes = new Node[childNodeCount];
                Values = new Dictionary<TKey, TValue>(keyComparer);
            }
            
            public Node[] ChildNodes { get; private set; }
            
            public Dictionary<TKey, TValue> Values { get; private set; }
        }

        private class LookupResult
        {
            public LookupResult(bool isKeyPresent, TValue value)
            {
                IsKeyPresent = isKeyPresent;
                Value = value;
            }
            public bool IsKeyPresent { get; private set; }
            public TValue Value { get; private set; }
        }
    }
}
