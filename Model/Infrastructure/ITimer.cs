using System;
using System.Threading.Tasks;

namespace Model.Infrastructure
{
    public interface ITimer
    {
        void SetTimeout(Action callback, int timeoutMs);
    }
}