using System.Linq;

namespace AOMapper.Helpers
{
    internal class ArgArray
    {
        private readonly object[] _argArray;

        public ArgArray(params object[] args)
        {
            this._argArray = args;
        }

        public override bool Equals(object obj)
        {
            // cast object to object array
            ArgArray comparedObject = obj as ArgArray;
            if (comparedObject == null) return false;

            // compare the array lengths
            if (comparedObject._argArray.Length == this._argArray.Length)
            {
                return !_argArray.Where((t, i) => !t.Equals(comparedObject._argArray[i])).Any();
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                for (int i = 0; i < _argArray.Length; i++)
                {
                    hash = hash * 16777619 ^ _argArray[i].GetHashCode();
                }                              
                return hash;
            }            
        }
    }
}
