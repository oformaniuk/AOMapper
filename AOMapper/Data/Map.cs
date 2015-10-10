namespace AOMapper.Data
{
    internal class Map<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public Map(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}