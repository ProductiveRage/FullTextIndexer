using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FullTextIndexer.Core.Indexes.TernarySearchTree
{
#if NET45
    [Serializable]
#endif
	public class TernarySearchTreeDictionary<TValue> : IEnumerable<string>
    {
		/// <summary>
		/// This delegate is used by the Add method to handle cases where data for a token exists in the current tree and in the data that is to be added to it. It
		/// will never be called with null current or toAdd references. It may return null if the two data items somehow cancel each other out and the token should
		/// not be recorded.
		/// </summary>
		public delegate TValue Combiner(TValue current, TValue toAdd);
		private static readonly Combiner _takeNewValue = (current, toAdd) => toAdd;

		private Node _root;
        private IStringNormaliser _keyNormaliser;
		public TernarySearchTreeDictionary(IEnumerable<KeyValuePair<string, TValue>> data, IStringNormaliser keyNormaliser)
			: this(Add(root: null, keyNormaliser: keyNormaliser, data: data, combine: _takeNewValue), keyNormaliser) { }
		private TernarySearchTreeDictionary(Node rootIfAny, IStringNormaliser keyNormaliser)
		{
			if (keyNormaliser == null)
				throw new ArgumentNullException("keyNormaliser");

			_root = rootIfAny;
			_keyNormaliser = keyNormaliser;
		}

		/// <summary>
		/// This will never be null nor contain any nulls, empty strings or duplicate values
		/// </summary>
		public IEnumerable<string> GetAllNormalisedKeys()
        {
            if (_root == null)
                return new string[0]; // If root is null then there is no data
            return _root.GetAllNodes().Where(n => n.Key != null).Select(n => n.Key);
        }

        /// <summary>
        /// This will never be null
        /// </summary>
        public IEnumerable<TValue> GetAllValues()
        {
            if (_root == null)
                return new TValue[0]; // If root is null then there is no data
            return _root.GetAllNodes().Where(n => n.Key != null).Select(n => n.Value);
        }

        /// <summary>
        /// This will never be null
        /// </summary>
        public IStringNormaliser KeyNormaliser
        {
            get { return _keyNormaliser; }
        }

        /// <summary>
        /// Get the average ratio of Depth-to-Key-Length (Depth will always be greater or equal to the Key Length). The lower the value, the better balanced the
        /// tree and the better the performance should be (1 would the lowest value and would mean that all paths were optimal but this is not realistic with
        /// real data - less than 2.5 should yield excellent performance). This will be zero if there are no keys.
        /// </summary>
        public float GetBalanceFactor()
        {
            if (_root == null)
                return 0; // If root is null then there is no data
            return _root.GetAllNodes()
                .Where(n => n.Key != null)
                .Average(n => (float)n.GetDepth() / (float)n.Key.Length);
        }

        /// <summary>
        /// This will throw an exception for a key not present in the data
        /// </summary>
        public TValue this[string key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                    throw new KeyNotFoundException();
                return value;
            }
        }

        /// <summary>
        /// This will return true if the specified key was found and will set the value output parameter to the corresponding value. If it return false then the
        /// value output parameter should not be considered to be defined.
        /// </summary>
        public bool TryGetValue(string key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (_root == null)
            {
                // If root is null then there is no data
                value = default(TValue);
                return false;
            }

            var normalisedKey = _keyNormaliser.GetNormalisedString(key);
            if (normalisedKey != "")
            {
                var node = _root;
                var index = 0;
                while (true)
                {
                    if (node.Character == normalisedKey[index])
                    {
                        index++;
                        if (index == normalisedKey.Length)
                        {
                            if (node.Key != null)
                            {
                                value = node.Value;
                                return true;
                            }
                            break;
                        }
                        node = node.MiddleChild;
                    }
                    else if (normalisedKey[index] < node.Character)
                        node = node.LeftChild;
                    else
                        node = node.RightChild;
                    if (node == null)
                        break;
                }
            }
            value = default(TValue);
            return false;
        }

		/// <summary>
		/// This will return a new TernarySearchTreeDictionary the combines the existing data with the new - if any tokens exist in the current tree and in the new
		/// data then the new data content will replace what was previously recorded, if a different method of combination is desired then call the Add method overload
		/// that takes a Combiner delegate. It will throw an exception for a null data reference, if the set contains any null keys, if any keys are transformed to empty
		/// string when passed through the keyNormaliser or if any duplicate keys would arise from combining the new data with the existing.
		/// </summary>
		public TernarySearchTreeDictionary<TValue> Add(IEnumerable<KeyValuePair<string, TValue>> data) => Add(data, _takeNewValue);

		/// <summary>
		/// This will return a new TernarySearchTreeDictionary the combines the existing data with the new - if any tokens exist in the current tree and in the new data
		/// then it will be combined into a single value by calling the provided Combiner delegate. It will throw an exception for a null data reference, if the set contains
		/// any null keys, if any keys are transformed to empty string when passed through the keyNormaliser or if any duplicate keys would arise from combining the new data
		/// with the existing.
		/// </summary>
		public TernarySearchTreeDictionary<TValue> Add(IEnumerable<KeyValuePair<string, TValue>> data, Combiner combine)
		{
			if (data == null)
                throw new ArgumentNullException("data");
			if (combine == null)
				throw new ArgumentNullException(nameof(combine));

			if (!data.Any())
                return this;

			var newRoot = Add(_root, _keyNormaliser, data, combine);
			return new TernarySearchTreeDictionary<TValue>(newRoot, _keyNormaliser);
        }

		private static Node Add(Node root, IStringNormaliser keyNormaliser, IEnumerable<KeyValuePair<string, TValue>> data, Combiner combine)
        {
            if (keyNormaliser == null)
                throw new ArgumentNullException("keyNormaliser");
            if (data == null)
                throw new ArgumentNullException("keys");
			if (combine == null)
				throw new ArgumentNullException(nameof(combine));

			if (!data.Any())
                return root;

            if (root != null)
                root = root.Clone();
            foreach (var entry in data)
            {
                var key = entry.Key;
                if (key == null)
                    throw new ArgumentException("Null key encountered in data");
                var normalisedKey = keyNormaliser.GetNormalisedString(key);
                if (normalisedKey == "")
                    throw new ArgumentException("key value results in blank string when normalised: " + key);

                if (root == null)
                    root = new Node() { Character = normalisedKey[0] };

                var node = root;
                var index = 0;
                while (true)
                {
                    if (node.Character == normalisedKey[index])
                    {
                        index++;
                        if (index == normalisedKey.Length)
                        {
							node.Value = ((node.Value != null) && (entry.Value != null))
								? combine(node.Value, entry.Value)
								: ((node.Value != null) ? node.Value : entry.Value);
							node.Key = (node.Value == null) ? null : normalisedKey; // If we ended up with a null Value then the Combiner may have removed it, in which case we should set the Key to null as well
							break;
                        }
                        if (node.MiddleChild == null)
                            node.MiddleChild = new Node() { Character = normalisedKey[index], Parent = node };
                        node = node.MiddleChild;
                    }
                    else if (normalisedKey[index] < node.Character)
                    {
                        if (node.LeftChild == null)
                            node.LeftChild = new Node() { Character = normalisedKey[index], Parent = node };
                        node = node.LeftChild;
                    }
                    else
                    {
                        if (node.RightChild == null)
                            node.RightChild = new Node() { Character = normalisedKey[index], Parent = node };
                        node = node.RightChild;
                    }
                }
            }
            return root;
        }

        /// <summary>
        /// This will return a new TernarySearchTreeDictionary without data relating to the specified keys. It will throw an exception for a null keys reference
        /// or if the set contains any null values.
        /// </summary>
        public TernarySearchTreeDictionary<TValue> Remove(IEnumerable<string> keys)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");

            if (_root == null)
                return this; // If root is null then there is no data

            var newRoot = _root.Clone();
            var removedAnyKeys = false;
            foreach (var key in keys)
            {
                if (key == null)
                    throw new ArgumentException("Null reference encountered in keys set");

                // We will never store keys that are empty when normalised, so can safely skip any that do
                var normalisedKey = _keyNormaliser.GetNormalisedString(key);
                if (normalisedKey == "")
                    continue;

                var node = newRoot;
                var matchedKeyLength = 0;
                while (true)
                {
                    if (node.Character == normalisedKey[matchedKeyLength])
                    {
                        matchedKeyLength++;
                        if (matchedKeyLength == normalisedKey.Length)
                            break;
                        node = node.MiddleChild;
                    }
                    else if (normalisedKey[matchedKeyLength] < node.Character)
                        node = node.LeftChild;
                    else
                        node = node.RightChild;
                    if (node == null)
                        break;
                }

                // If we successfully located the key's Node then we need to mark the Node as no longer containing that key (or its value)
                if ((matchedKeyLength == normalisedKey.Length) && (node.Key != null))
                {
                    node.Key = null;
                    node.Value = default(TValue);
                    removedAnyKeys = true;
                }
            }

            // If no keys were actually removed then return this instance
            if (!removedAnyKeys)
                return this;

            // Otherwise we need to prune out any dead Nodes (Nodes that have don't lead to any key) and return a new instance with this data
            Prune(newRoot);
			return new TernarySearchTreeDictionary<TValue>(newRoot, _keyNormaliser);
        }

		/// <summary>
		/// This will return a new TernarySearchTreeDictionary where every value is transformed using the specified delegate. If the delegate returns null then the
		/// value will be removed. It will throw an exception for a null transformer reference.
		/// </summary>
		public TernarySearchTreeDictionary<TValue> Update(Func<TValue, TValue> transformer)
		{
			if (transformer == null)
				throw new ArgumentNullException(nameof(transformer));

			if (_root == null)
				return this;

			var newRoot = _root.Clone();
			newRoot.Update(transformer);
			//Prune(newRoot); // 2018-12-15: Disabling this for now because this method is too slow already and other methods will pick up Prune calls!
			return new TernarySearchTreeDictionary<TValue>(newRoot, _keyNormaliser);
		}

		/// <summary>
		/// This will set to null any descendant Nodes that don't contain values (either within themselves or within any further descendant). It will return true if
		/// any pruning was performed and false if not.
		/// </summary>
		private bool Prune(Node node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            var actionTaken = false;
            if (node.LeftChild != null)
            {
                actionTaken = actionTaken || Prune(node.LeftChild);
                if (!node.LeftChild.ContainsAnyValues())
                    node.LeftChild = null;
            }
            if (node.MiddleChild != null)
            {
                actionTaken = actionTaken || Prune(node.MiddleChild);
                if (!node.MiddleChild.ContainsAnyValues())
                    node.MiddleChild = null;
            }
            if (node.RightChild != null)
            {
                actionTaken = actionTaken || Prune(node.RightChild);
                if (!node.RightChild.ContainsAnyValues())
                    node.RightChild = null;
            }
            return actionTaken;
        }

        /// <summary>
        /// This will never return null, the returned dictionary will have this instance's KeyNormaliser as its comparer
        /// </summary>
        public Dictionary<string, TValue> ToDictionary()
        {
            var dictionary = new Dictionary<string,TValue>(_keyNormaliser);
            if (_root != null)
            {
                // If root is null then there is no data
                foreach (var keyNode in _root.GetAllNodes().Where(n => n.Key != null))
                    dictionary.Add(keyNode.Key, keyNode.Value);
            }
            return dictionary;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return GetAllNormalisedKeys().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#if NET45
    [Serializable]
#endif
		private class Node
        {
            public char Character { get; set; }
            public Node LeftChild { get; set; }
            public Node MiddleChild { get; set; }
            public Node RightChild { get; set; }
            public Node Parent { get; set; }

            /// <summary>
            /// This will be null if this Node is not a Key-describing Node
            /// </summary>
            public string Key { get; set; }
            public TValue Value { get; set; }

            /// <summary>
            /// Does the specified Node or any descendant have a true IsKey value? This will throw an exception for a null node.
            /// </summary>
            public bool ContainsAnyValues()
            {
                return (
                    (Key != null) ||
                    ((LeftChild != null) && LeftChild.ContainsAnyValues()) ||
                    ((MiddleChild != null) && MiddleChild.ContainsAnyValues()) ||
                    ((RightChild != null) && RightChild.ContainsAnyValues())
                );
            }

            /// <summary>
            /// Returns a set containing the current node and all of its descendants (if any). It will never return null nor a set with any null entries.
            /// </summary>
            public IEnumerable<Node> GetAllNodes()
            {
                foreach (var node in new[] { LeftChild, MiddleChild, RightChild })
                {
					if (node == null)
						continue;

					yield return node;
					foreach (var childNode in node.GetAllNodes())
						yield return childNode;
				}
			}

            public int GetDepth()
            {
                var depth = 1;
                var node = this;
                while (node.Parent != null)
                {
                    depth++;
                    node = node.Parent;
                }
                return depth;
            }

			public Node Clone()
			{
				var newNode = new Node()
				{
					Character = Character,
					LeftChild = LeftChild?.Clone(),
					MiddleChild = MiddleChild?.Clone(),
					RightChild = RightChild?.Clone(),
					Parent = Parent,
					Key = Key,
					Value = Value
				};
				if (newNode.LeftChild != null)
					newNode.LeftChild.Parent = newNode;
				if (newNode.MiddleChild != null)
					newNode.MiddleChild.Parent = newNode;
				if (newNode.RightChild != null)
					newNode.RightChild.Parent = newNode;
				return newNode;
			}

			public void Update(Func<TValue, TValue> transformer)
			{
				if (transformer == null)
					throw new ArgumentNullException(nameof(transformer));

				bool removedItem;
				TValue newValue;
				if (Key == null)
				{
					// If the Key is null then it means that this node is part of a chain but doesn't have its own Key and Value, in which case leave
					// Value as whatever it currently is on the cloned node (should be default(T))
					newValue = Value;
					removedItem = false; // This node doesn't represent a value and so we haven't removed anything
				}
				else
				{
					newValue = transformer(Value);
					removedItem = (newValue == null);
				}
				Key = removedItem ? null : Key;
				Value = newValue;
				LeftChild?.Update(transformer);
				MiddleChild?.Update(transformer);
				RightChild?.Update(transformer);
			}
		}
	}
}