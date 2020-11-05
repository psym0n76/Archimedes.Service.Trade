using System.Threading;

namespace Archimedes.Service.Price
{
    public interface IPriceSubscriber
    {
        void Consume(CancellationToken cancellationToken);
    }
}