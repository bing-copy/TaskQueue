using System;
using System.Threading.Tasks;

namespace TaskQueue
{
    public class TaskQueueOptions
    {
        public int MaxThreads { get; set; }
        public int Interval { get; set; }
        /// <summary>
        /// TaskData, Exception, ReexecTaskData
        /// </summary>
        public Func<TaskData, Exception, Task<bool>> OnException { get; set; }
    }
}
