namespace TaskQueue
{
    public abstract class TaskData
    {
        public int TryTimes { get; set; }
        public bool ExecuteImmediately { get; set; }
    }
}
