using System.Threading.Tasks;

namespace demunity.lib.Tasks
{
    public static class TaskExtensions
    {
        public static T SafeGetResult<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}