using System;
using System.Threading.Tasks;

namespace TaskQueue
{
    public class TaskQueueOptions
    {
        /// <summary>
        /// Max concurrent count of running tasks.
        /// </summary>
        public int MaxThreads { get; set; }
        /// <summary>
        /// Interval(ms) for starting a new task.
        /// </summary>
        public int Interval { get; set; }
    }
}
