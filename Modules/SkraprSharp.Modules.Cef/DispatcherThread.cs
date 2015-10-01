namespace SkraprSharp.Modules.Cef
{
    using CefSharp;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;

    public sealed class DispatcherThread : DisposableResource
    {
        private DispatcherFrame m_frame;
        private ManualResetEvent m_startedEvent;
        private Thread m_thread;
        private Dispatcher m_dispatcher;

        public DispatcherThread()
        {
            using (m_startedEvent = new ManualResetEvent(false))
            {
                m_thread = new Thread(Run);
                m_thread.SetApartmentState(ApartmentState.STA);
                m_thread.Name = "WebBrowserDispatcherThread";
                m_thread.Start();

                m_startedEvent.WaitOne();
            }
        }

        public TaskFactory TaskFactory
        {
            get;
            private set;
        }

        public Task StartNew(Action action)
        {
            return TaskFactory.StartNew(action);
        }

        public Task<T> StartNew<T>(Func<T> action)
        {
            return TaskFactory.StartNew(action);
        }

        private void Run()
        {
            m_frame = new DispatcherFrame(true);

            m_dispatcher = Dispatcher.CurrentDispatcher;

            Action action = () =>
            {
                TaskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
                m_startedEvent.Set();
            };

            m_dispatcher.BeginInvoke(action);

            Dispatcher.PushFrame(m_frame);
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (m_frame != null)
            {
                m_frame.Continue = false;
                m_frame = null;
            }

            m_dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);

            if (!m_thread.Join(TimeSpan.FromSeconds(5)))
            {
                m_thread.Abort();

                m_thread.Interrupt();
            }

            m_thread = null;

            base.DoDispose(isDisposing);
        }
    }
}
