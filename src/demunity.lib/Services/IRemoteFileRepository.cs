using System;
using System.Threading.Tasks;

namespace demunity.lib
{
    public interface IRemoteFileRepository
    {
        Task<Uri> GetUploadUri(string fileName);
    }
}
