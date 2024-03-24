﻿using System.Diagnostics;

namespace T3.Editor.External.Truncon.Collections
{
    internal class OrderedDictionaryDebugView<TKey, TValue>
    {
        private readonly OrderedDictionary<TKey, TValue> dictionary;

        public OrderedDictionaryDebugView(OrderedDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Items => dictionary.ToArray();
    }
}
