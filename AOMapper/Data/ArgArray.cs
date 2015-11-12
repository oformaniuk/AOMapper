namespace AOMapper.Data
{
    internal class ArgArray
    {
        private readonly object[] _argArray;
        private readonly int _hashCode;

        public ArgArray(params object[] args)
        {
            _argArray = args;
            _hashCode = getHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ArgArray)) return false;

            return _hashCode == (obj as ArgArray)._hashCode;
        }

        private int getHashCode()
        {
            unchecked
            {
                var hash = (int) 2166136261;
                for (var i = 0; i < _argArray.Length; i++)
                {
                    hash = hash*16777619 ^ _argArray[i].GetHashCode();
                }
                return hash;
            }
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}