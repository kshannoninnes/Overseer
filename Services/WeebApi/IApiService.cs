
using Overseer.Models;
using Overseer.Services.Logging;
using System.Threading.Tasks;

namespace Overseer.Services.WeebApi
{
    public interface IApiService
    {
        public Task<OverseerMedia> GetMediaAsync(string search, string type);
    }
}
