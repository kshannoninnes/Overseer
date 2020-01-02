using Overseer.Models;
using System.Threading.Tasks;

namespace Overseer.Services
{
    public interface IMediaFetcher
    {
        Task<OverseerMedia> GetMediaAsync(string title);
    }
}
