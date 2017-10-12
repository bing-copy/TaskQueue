using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TaskQueue
{
    public abstract class TaskQueue
    {
        public TaskQueueOptions Options;
        protected int SuccessCount;
        protected int TotalCount;
        protected int FailureCount;
        public int CurrentThreadCount;
        public bool Finished { get; set; }
        protected ILogger Logger;
        protected EventId EventId;

        /// <summary>
        /// Current state for this task, you can use <see cref="State"/>.ToString() to get the output.
        /// </summary>
        public virtual object State =>
            string.Format(
                $"[{GetType().Name}]Total: {TotalCount}, Success: {SuccessCount}, Failed: {FailureCount}, Threads: {CurrentThreadCount}/{Options.MaxThreads}");

        protected TaskQueue(TaskQueueOptions options) : this(options, null)
        {
        }

        protected TaskQueue(TaskQueueOptions options, ILoggerFactory loggerFactory)
        {
            Options = options;
            if (loggerFactory == null)
            {
                loggerFactory = new LoggerFactory();
                loggerFactory.AddConsole();
            }
            var type = GetType();
            Logger = loggerFactory.CreateLogger(type);
            EventId = new EventId(0, type.Name);
        }

        public abstract Task<List<TaskData>> ExecuteAsync(TaskData taskData);

        public abstract bool MatchedTaskData(TaskData taskData);

        public virtual bool CanExecuteData()
        {
            return !Finished && (Options.MaxThreads == 0 || Options.MaxThreads > CurrentThreadCount);
        }
    }

    public abstract class TaskQueue<TOptions, TTaskData> : TaskQueue
        where TOptions : TaskQueueOptions where TTaskData : TaskData
    {
        protected new TOptions Options;

        protected TaskQueue(TOptions options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        {
            Options = options;
        }

        protected TaskQueue(TOptions options) : this(options, null)
        {

        }

        protected abstract Task<List<TaskData>> ExecuteAsyncInternal(TTaskData taskData);

        public override async Task<List<TaskData>> ExecuteAsync(TaskData taskData)
        {
            Interlocked.Increment(ref TotalCount);
            List<TaskData> newTaskData = null;
            var data = (TTaskData) taskData;
            try
            {
                data.TryTimes++;
                newTaskData = await ExecuteAsyncInternal(data);
                Interlocked.Increment(ref SuccessCount);
            }
            catch (Exception e)
            {
                Logger.LogError(EventId, e,
                    $"An error occured while executing task data: {e.Message}, data: {JsonConvert.SerializeObject(data)}");
                // Re-execute task data later
                Interlocked.Increment(ref FailureCount);
                if (Options.OnException != null)
                {
                    var reExecData = await Options.OnException(data, e);
                    if (reExecData)
                    {
                        newTaskData = new List<TaskData> {data};
                    }
                }

            }
            return newTaskData;
        }

        public override bool MatchedTaskData(TaskData taskData)
        {
            return taskData is TTaskData;
        }
    }
}