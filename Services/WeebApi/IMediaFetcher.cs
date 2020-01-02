using System.Threading.Tasks;

using Overseer.Models;

namespace Overseer.Services.WeebApi
{
    public interface IMediaFetcher
    {
        Task<OverseerMedia> GetMediaAsync(string title);
    }
}
