namespace TaskDemo
{
    using Microsoft.VisualStudio.Threading;
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncManualResetEvent = Nito.AsyncEx.AsyncManualResetEvent;
    using AsyncManualResetEventVS = Microsoft.VisualStudio.Threading.AsyncManualResetEvent;

    public class AsyncTest
    {
        /// <summary>
        /// The timeout value in milliseconds.
        /// </summary>
        public static int Timeout => 120000;

        /// <summary>
        /// Waits until the specified object is signaled.
        /// </summary>
        /// <param name="sem">The semaphore to wait on.</param>
        public async static Task StartWaitSemaphoreSlimAsync(SemaphoreSlim sem)
        {
            if (await sem.WaitAsync(Timeout))
            {
                Print($"Wait completed on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
            else
            {
                Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
            }

            SimulateWork();
        }

        /// <summary>
        /// Waits until the specified object is signaled.
        /// </summary>
        /// <param name="sem">The semaphore to wait on.</param>
        /// <param name="sw">The stopwatch instance used to measure when this task got scheduled.
        public static void StartWaitSemaphoreSlimNoAsync(SemaphoreSlim sem, Stopwatch sw)
        {
            var started = sw.Elapsed;
            if (sem.Wait(Timeout))
            {
                var tid = Thread.CurrentThread.ManagedThreadId;
                Print($"Completed on thread {tid}. (Start delay: {started.TotalMilliseconds} ms.)");
            }
            else
            {
                Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
            }

            SimulateWork();
        }

        /// <summary>
        /// Waits until the specified object is signaled.
        /// </summary>
        /// <param name="sem">The semaphore to wait on.</param>
        public static void StartWaitSemaphoreSlimSync(SemaphoreSlim sem)
        {
            if (sem.Wait(Timeout))
            {
                Print($"Wait completed on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
            else
            {
                Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
            }

            SimulateWork();
        }

        /// <summary>
        /// Waits until the specified object is signaled.
        /// </summary>
        /// <param name="sem">The semaphore to wait on.</param>
        public static void StartMainManualResetEventSync(ManualResetEvent mre)
        {
            if (mre.WaitOne())
            {
                Print($"Wait completed on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
            else
            {
                Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
        }

        /// <summary>
        /// Waits until the specified object is signaled.
        /// </summary>
        /// <param name="mre">The reset event to wait on.</param>
        public async static Task<bool> StartWaitAsyncManualResetEventAsync(AsyncManualResetEvent mre)
        {
            Task t1 = mre.WaitAsync();
            Task t2 = Task.Delay(Timeout);

            Task completedTask = await Task.WhenAny(t1, t2);

            bool result = completedTask == t1;

            if (completedTask == t1)
            {
                Print($"Wait completed on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
            else
            {
                Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
            }

            SimulateWork();

            return result;
        }

        /// <summary>
        /// Waits until the specified object is signaled.
        /// </summary>
        /// <param name="cst">The object to wait on.</param>
        public async static Task StartWaitCompletionSourceTestAsync(CompletionSourceTest cst)
        {
            if (await cst.WaitForItAsync())
            {
                Print($"Wait completed on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
            else
            {
                Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
            }

            SimulateWork();
        }

        /// <summary>
        /// Waits until the specified object is signaled.
        /// </summary>
        /// <param name="mre">The reset event to wait on.</param>
        /// <param name="cancelToken">The cancellation token to use.</param>
        public async static Task<bool> StartWaitAsyncManualResetEventWithCancelTokenAsync(AsyncManualResetEvent mre, CancellationToken cancelToken)
        {
            bool result = false;

            try
            {
                await mre.WaitAsync(cancelToken);
                result = true;
                Print($"Wait completed on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
            catch (Exception)
            {
                Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
            }

            SimulateWork();

            return result;
        }

        /// <summary>
        /// Waits until the specified object is signaled.
        /// </summary>
        /// <param name="sem">The semaphore to wait on.</param>
        /// <param name="cancelToken">The cancellation token to use.</param>
        public async static Task<bool> StartWwaitSemaphoreSlimWithCancelTokenAsync(SemaphoreSlim sem, CancellationToken cancelToken)
        {
            bool result = false;

            try
            {
                await sem.WaitAsync(cancelToken);
                result = true;
                Print($"Wait completed on thread {Thread.CurrentThread.ManagedThreadId}.");
            }
            catch (Exception e)
            {
                Print(e);
                Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
            }

            SimulateWork();

            return result;
        }

        /// <summary>
        /// Waits until the specified object is signaled.
        /// </summary>
        /// <param name="mre">The reset event to wait on.</param>
        /// <param name="sw">The stopwatch instance used to measure when this task got scheduled.
        public async static Task<bool> StartWaitAsyncManualResetEventVSAsync(
            AsyncManualResetEventVS mre,
            Stopwatch sw)
        {
            var started = sw.Elapsed;
            bool result = false;

            try
            {
                await mre.WaitAsync().WithTimeout(TimeSpan.FromMilliseconds(Timeout));
                var tid = Thread.CurrentThread.ManagedThreadId;
                result = true;
                Print($"Completed on thread {tid}. (Start delay: {started.TotalMilliseconds} ms.)");
            }
            catch (TimeoutException)
            {
                Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
            }

            SimulateWork();

            return result;
        }

        public static Task<bool> StartWaitManualResetEventVSAsyncWithSyncWaitAsync(AsyncManualResetEventVS mre)
        {
            bool result = false;

            var t = Task.Factory.StartNew(() =>
            {
                try
                {
                    mre.WaitAsync().WithTimeout(TimeSpan.FromMilliseconds(Timeout)).Wait();
                    result = true;
                    Print($"Wait completed on thread {Thread.CurrentThread.ManagedThreadId}.");
                }
                catch (TimeoutException)
                {
                    Print($"Wait timed out on thread {Thread.CurrentThread.ManagedThreadId}.");
                }

                SimulateWork();

                return result;
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current);

            return t;
        }

        /// <summary>
        /// Gets how many thread pool threads are currently running.
        /// </summary>
        /// <returns></returns>
        public static int GetRunningThreadPoolThreads()
        {
            int max, available, ignore;
            ThreadPool.GetMaxThreads(out max, out ignore);
            ThreadPool.GetAvailableThreads(out available, out ignore);
            int running = max - available;
            return running;
        }

        /// <summary>
        /// Simulates doing something useful.
        /// </summary>
        private static void SimulateWork()
        {
            Thread.Sleep(250);
        }

        /// <summary>
        /// Writes the specified message with the thread id pre-pended.
        /// </summary>
        /// <param name="message">The message to print.</param>
        private static void Print(string message)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {message}");
        }

        /// <summary>
        /// Writes the specified object with the thread id pre-pended.
        /// </summary>
        /// <param name="o">The message to print.</param>
        private static void Print(object o)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {o.ToString()}");
        }
    }
}
