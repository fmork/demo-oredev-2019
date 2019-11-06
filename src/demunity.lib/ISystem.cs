namespace demunity.lib
{
    public interface ISystem
    {
        IEnvironment Environment { get; }
        ISystemTime Time { get; }
    }
}