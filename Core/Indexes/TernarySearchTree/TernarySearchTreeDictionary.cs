using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FullTextIndexer.Core.Indexes.TernarySearchTree
{
    [Serializable]
    public class TernarySearchTreeDictionary<TValue> : IEnumerable<string>
    {
        private Node _root;
        private IStringNormaliser _keyNormaliser;
        public TernarySearchTreeDictionary(IEnumerable<KeyValuePair<string, TValue>> data, IStringNormaliser keyNormaliser)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (keyNormaliser == null)
                throw new ArgumentNullException("keyNormaliser");

            _root = Add(null, keyNormaliser, data);
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
        /// This will return a new TernarySearchTreeDictionary the combines the existing data with the new. It willthrow an exception for a null data reference,
        /// if the set contains any null keys, if any keys are transformed to empty string when passed through the keyNormaliser or if any duplicate keys would
        /// arise from combining the new data with the existing.
        /// </summary>
        public TernarySearchTreeDictionary<TValue> Add(IEnumerable<KeyValuePair<string, TValue>> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!data.Any())
                return this;
            return new TernarySearchTreeDictionary<TValue>(new Dictionary<string, TValue>(), _keyNormaliser)
            {
                _root = Add(_root, _keyNormaliser, data)
            };
        }

        /// <summary>
        /// Combine additional data with an existing root's content, returning a new Node (unless zero data entries were specified, in which case the original
        /// reference will be returned). This will throw an exception for a null keyNormalised or data (or if any keys in the data return null, empty string
        /// or duplicates when run through the keyNormaliser). If a null root is specified then a new root will be generated.
        /// </summary>
        private static Node Add(Node root, IStringNormaliser keyNormaliser, IEnumerable<KeyValuePair<string, TValue>> data)
        {
            if (keyNormaliser == null)
                throw new ArgumentNullException("keyNormaliser");
            if (data == null)
                throw new ArgumentNullException("keys");

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
                            node.Key = normalisedKey;
                            node.Value = entry.Value;
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
            return new TernarySearchTreeDictionary<TValue>(new Dictionary<string, TValue>(), _keyNormaliser)
            {
                _root = newRoot,
            };
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

        [Serializable]
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
                var nodes = new List<Node> { this };
                foreach (var childNode in new[] { LeftChild, MiddleChild, RightChild })
                {
                    if (childNode != null)
                        nodes.AddRange(childNode.GetAllNodes());
                }
                return nodes;
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

            /// <summary>
            /// Returns a cloned copy of the node, deep-cloning all data. This will throw an exception for a null node.
            /// </summary>
            public Node Clone()
            {
                // To clone it, we copy Character, Parent, Key and Value onto a new instance and then clone the child nodes (if non-null). After this the
                // Parents on the child nodes will have to be set to the new instance so that a new chain can be formed. This should be called against
                // the root node such that an entirely new tree is formed.
                var newNode = new Node()
                {
                    Character = Character,
                    LeftChild = (LeftChild == null) ? null : LeftChild.Clone(),
                    MiddleChild = (MiddleChild == null) ? null : MiddleChild.Clone(),
                    RightChild = (RightChild == null) ? null : RightChild.Clone(),
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
        }
    }
}
