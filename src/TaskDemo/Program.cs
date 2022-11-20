namespace TaskDemo
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.MemoryMappedFiles;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncManualResetEventVS = Microsoft.VisualStudio.Threading.AsyncManualResetEvent;

    class Program
    {
        private static int TaskCount = 1000;
        private static int? MinThreads = 100;
        private static CancellationTokenSource cts = new();

        private static async Task Main(string[] args)
        {
            try
            {
                Console.CancelKeyPress += (s, e) =>
                {
                    DoCancellation();
                };

                if (args.Length > 0)
                {
                    if (!int.TryParse(args[0], out var minthreads))
                    {
                        Console.WriteLine("Input should be an integer.");
                        return;
                    }

                    MinThreads = minthreads;

                    if (args.Length > 1 && args[1] == "async")
                    {
                        await TestWithAsyncManualResetEventVSAsync();
                    }
                    else
                    {
                        TestWithSemaphoreSlimNoAsync();
                    }

                    return;
                }
            }
            finally
            {
                DoCancellation();
            }
        }

        private static void DoCancellation()
        {
            cts.Cancel();
            Thread.Sleep(500);
        }

        private static void TestWithSemaphoreSlimNoAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();
            Console.Clear();
            Print(nameof(TestWithSemaphoreSlimNoAsync));

            DoMonitorThreadPoolThreads(nameof(TestWithSemaphoreSlimNoAsync), cts.Token);

            List<SemaphoreSlim> sems = new List<SemaphoreSlim>();
            List<Task> tasks = new List<Task>();

            SetMinThreads();

            for (int i = 0; i < TaskCount; i++)
            {
                SemaphoreSlim sem = new SemaphoreSlim(0, 1);
                sems.Add(sem);
                var startTimer = Stopwatch.StartNew();
                Task t = Task.Factory.StartNew(
                    () => AsyncTest.StartWaitSemaphoreSlimNoAsync(sem, startTimer),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.Default);

                tasks.Add(t);
            }

            Print($"{TaskCount} tasks started.");

            Print("Press <Enter> to signal threads to complete.");
            WaitForEnter();

            sw.Restart();

            foreach (SemaphoreSlim sem2 in sems)
            {
                sem2.Release();
            }

            Task.WaitAll(tasks.ToArray());

            Print($"Finished after {sw.ElapsedMilliseconds} ms. Press <Enter> to continue.");
            WaitForEnter();

            cts.Cancel();
        }

        private static async Task TestWithAsyncManualResetEventVSAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();

            Console.Clear();
            Print(nameof(TestWithAsyncManualResetEventVSAsync));

            List<AsyncManualResetEventVS> resetEvents = new List<AsyncManualResetEventVS>();

            DoMonitorThreadPoolThreads(nameof(TestWithAsyncManualResetEventVSAsync), cts.Token);

            List<Task> tasks = new List<Task>();

            SetMinThreads();

            for (int i = 0; i < TaskCount; i++)
            {
                AsyncManualResetEventVS amre = new AsyncManualResetEventVS();
                resetEvents.Add(amre);
                var startTimer = Stopwatch.StartNew();
                Task t = AsyncTest.StartWaitAsyncManualResetEventVSAsync(amre, startTimer);

                tasks.Add(t);
            }

            Print($"{TaskCount} tasks started.");

            Print("Press <Enter> to signal threads to complete.");
            WaitForEnter();

            sw.Restart();

            foreach (AsyncManualResetEventVS amre in resetEvents)
            {
                amre.Set();
            }

            await Task.WhenAll(tasks);

            Print($"Finished after {sw.ElapsedMilliseconds} ms. Press <Enter> to continue.");
            WaitForEnter();

            cts.Cancel();
        }

        private static void MonitorThreadPoolThreads(CancellationToken ct)
        {
            var sleepTime = 250;
            var commfile = "task_demo_comm";
            using var mmf = MemoryMappedFile.CreateOrOpen(commfile, 8);
            using var view = mmf.CreateViewAccessor(0, 8);

            view.Write(4, true);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    int threads = AsyncTest.GetRunningThreadPoolThreads();
                    Console.Title = $"{threads} pool threads.";
                    view.Write(0, threads);

                    Thread.Sleep(sleepTime);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OH NO {e}");
                }
            }

            view.Write(0, -1);
        }

        private static void DoMonitorThreadPoolThreads(string operation, CancellationToken cancelToken)
        {
            Console.Title = operation;

            Task.Factory.StartNew(
                () =>
                MonitorThreadPoolThreads(cancelToken),
                cancelToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Ignore();
        }

        private static void WaitForEnter()
        {
            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    break;
                }
            }
        }

        private static void SetMinThreads()
        {
            int minThreads = MinThreads switch
            {
                not null => MinThreads.Value,
                _ => Math.Min(1000, TaskCount)
            };

            ThreadPool.SetMinThreads(minThreads, minThreads);
        }

        private static void Print(string message)
        {
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {message}");
        }
    }
}
