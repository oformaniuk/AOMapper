using AOMapper.Data.Keys;

namespace AOMapper.Data
{
    public class CallStackNode
    {
        public StringKey Route { get; set; }
        public dynamic Value { get; set; }
    }
}