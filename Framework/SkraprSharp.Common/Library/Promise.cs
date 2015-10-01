namespace SkraprSharp.Library
{
    using NiL.JS.BaseLibrary;
    using NiL.JS.Core;
    using NiL.JS.Core.Functions;
    using NiL.JS.Core.Modules;
    using NiL.JS.Core.TypeProxing;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [Serializable]
    public class Promise : CustomType
    {
        private Task<JSObject> m_promiseTask;
        private CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();
        private JSObject m_result = Undefined;

        [DoNotEnumerate]
        public Promise(Arguments args)
        {
            m_promiseTask = new Task<JSObject>(() =>
            {
                return Undefined;
            });
        }

        [DoNotEnumerate]
        public Promise(Function executor)
        {
            m_promiseTask = Task.Run(() =>
            {
                if (executor != null)
                {
                    var executorArguments = new Arguments();
                    executorArguments.Add(new ExternalFunction(Resolve));
                    executorArguments.Add(new ExternalFunction(Reject));

                    executor.Invoke(executorArguments);
                }

                return m_result;
            }, m_cancellationTokenSource.Token);
        }

        private JSObject Resolve(JSObject thisBind, Arguments args)
        {
            m_result = args[0];
            return Undefined;
        }

        private JSObject Reject(JSObject thisBind, Arguments args)
        {
            m_result = args[0];
            m_cancellationTokenSource.Cancel();

            var errorArgs = new Arguments();
            errorArgs.Add(m_result);

            throw new JSException(new Error(errorArgs));
        }

        [Hidden]
        public Promise(Task<JSObject> task)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            m_promiseTask = task;
        }

        [DoNotEnumerate]
        public Promise then(Arguments args)
        {
            var onFullfilledFunction = args[0] as Function;
            if (onFullfilledFunction != null)
            {
                m_promiseTask.ContinueWith(t =>
                {
                    if (!t.IsCompleted)
                        return;

                    var innerArgs = new Arguments();
                    innerArgs.Add(m_promiseTask.Result);

                    onFullfilledFunction.Invoke(innerArgs);
                });
            }

            var onRejectedFunction = args[1] as Function;
            if (onRejectedFunction != null)
            {
                m_promiseTask.ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        return;

                    var innerArgs = new Arguments();
                    innerArgs.Add(m_promiseTask.Result);

                    onRejectedFunction.Invoke(innerArgs);
                });
            }

            return this;
        }

        #region Static Methods

        [DoNotEnumerate]
        public static JSObject resolve(Arguments args)
        {
            var resolveValue = args[0];

            var resolveFunction = resolveValue as Function;
            if (resolveFunction != null)
            {
                return resolveFunction.Invoke(new Arguments());
            }

            var resolvePromise = resolveValue as Promise;
            if (resolvePromise != null)
            {
                if (resolvePromise.m_promiseTask.Status != TaskStatus.RanToCompletion)
                    return resolvePromise;

                if (resolvePromise.m_promiseTask.Status == TaskStatus.WaitingToRun)
                    resolvePromise.m_promiseTask.Start();

                resolvePromise.m_promiseTask.Wait();

                if (resolvePromise.m_promiseTask.Status == TaskStatus.RanToCompletion)
                    return resolvePromise.m_promiseTask.Result;

                return resolvePromise;
            }

            var resolveThenable = resolveValue["then"].Value as Function;
            if (resolveThenable != null)
            {
                return resolveThenable.Invoke(new Arguments());
            }

            return resolveValue;
        }
        #endregion

        public enum PromiseState
        {
            Pending,
            Fulfilled,
            Rejected
        }
    }
}
