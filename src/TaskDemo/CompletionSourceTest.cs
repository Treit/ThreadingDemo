namespace TaskDemo
{
    using System.Threading.Tasks;

    public class CompletionSourceTest
    {
        private TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<bool> WaitForItAsync()
        {
            return tcs.Task;
        }

        public void Signal()
        {
            tcs.SetResult(true);
        }
    }
}
