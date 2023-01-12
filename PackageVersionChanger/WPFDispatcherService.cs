using DevExpress.Mvvm;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TSP.PackageVersionChanger
{
    class WPFDispatcherService : IDispatcherService
    {
        private readonly Dispatcher _dispatcher;

        public WPFDispatcherService(Dispatcher dispatcher)
        {
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));
            _dispatcher = dispatcher;
        }

        public Task BeginInvoke(Action action)
        {
            DispatcherOperation op = _dispatcher.BeginInvoke(action);
            return op.Task;
        }

        public void Invoke(Action action)
            => _dispatcher.Invoke(action);
    }
}
