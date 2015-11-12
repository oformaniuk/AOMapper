namespace AOMapper.Interfaces.Keys
{
    public interface IKey<out T>
    {
        T Value { get; }
    }
}