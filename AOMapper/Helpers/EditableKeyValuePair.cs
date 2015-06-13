﻿namespace AOMapper.Helpers
{
    internal class EditableKeyValuePair<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public EditableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}