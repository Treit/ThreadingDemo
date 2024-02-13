﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadPoolLongRunning
{
    class Foo
    {
        public Foo(string val)
        {
            Val = val;
        }

        public string Val { get; set; }
    }

    class Program
    {
        private static int Running;
        private static ManualResetEventSlim mre = new();
        private const int Iterations = 30;

        static void Main(string[] args)
        {
            long Test1Time = Test(TaskCreationOptions.None);
            long Test2Time = Test(TaskCreationOptions.LongRunning);

            Console.WriteLine($"It took {Test1Time} ms. to get all of the tasks running WITHOUT LongRunning.");
            Console.WriteLine($"It took {Test2Time} ms. to get all of the tasks running WITH LongRunning.");

            GC.Collect(2, GCCollectionMode.Forced);

            Console.WriteLine($"Total threads: {Process.GetCurrentProcess().Threads.Count}");
            Console.WriteLine($"Running thread pool threads: {GetRunningThreadPoolThreads()}");

            Console.WriteLine("All done.");
            Console.WriteLine("Press <Enter> to exit.");
            Console.ReadKey();
        }

        static long Test(TaskCreationOptions options)
        {
            mre.Reset();
            Running = 0;

            Stopwatch sw = Stopwatch.StartNew();

            List<Task> tasks = new(Iterations);

            for (int i = 0; i < Iterations; i++)
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    Interlocked.Increment(ref Running);

                    while (!mre.IsSet)
                    {
                        if (Running >= Iterations)
                        {
                            mre.Set();
                            return;
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }, options);

                tasks.Add(t);
            }

            while (!mre.IsSet)
            {
                Thread.Sleep(500);
                Console.WriteLine($"Total threads: {Process.GetCurrentProcess().Threads.Count}");
                Console.WriteLine($"Running thread pool threads: {GetRunningThreadPoolThreads()}");

                foreach (var task in tasks)
                {
                    if (!task.IsCompleted)
                    {
                        continue;
                    }
                }
            }

            return sw.ElapsedMilliseconds;
        }

        /// <summary>
        /// Gets how many thread pool threads are currently running.
        /// </summary>
        /// <returns></returns>
        public static int GetRunningThreadPoolThreads()
        {
            ThreadPool.GetMaxThreads(out int max, out int _);
            ThreadPool.GetAvailableThreads(out int available, out _);
            int running = max - available;
            return running;
        }
    }
}
