using System.Threading.Tasks;

namespace Peery
{
    public interface IFileExchange
    {
        bool Finished { get; }
        bool Verbose { set; }
        SegmentedFile File { get; }
        Task Start();
        Task Pulse();
        void Stop();
    }
}