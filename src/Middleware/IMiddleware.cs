using System;

namespace OtterStore.Middleware
{
    public interface IMiddleware: IDisposable
    {
        void Initialize();
    }
}