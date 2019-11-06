namespace demunity.lib.Logging
{
    public interface ILogWriterFactory
    {
        ILogWriter<T> CreateLogger<T>();
    }
}