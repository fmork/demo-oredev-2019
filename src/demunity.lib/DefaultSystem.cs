namespace demunity.lib
{
    public class DefaultSystem : ISystem
    {
        public DefaultSystem(IEnvironment environment, ISystemTime systemTime)
        {
            Environment = environment;
            Time = systemTime;
        }

        public IEnvironment Environment { get; }
        public ISystemTime Time { get; }
    }
}