using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TaskQueue
{
    public class TaskQueuePool : ConcurrentBag<TaskQueue>
    {
        private int _currentThreadCount;
        private readonly TaskQueuePoolOptions _options;
        private ConcurrentQueue<TaskData> _queuedTaskData = new ConcurrentQueue<TaskData>();
        private TaskQueuePoolStatus _status = TaskQueuePoolStatus.Idle;
        protected ILogger Logger;
        protected EventId EventId;
        protected ILoggerFactory LoggerFactory;

        public TaskQueuePool(TaskQueuePoolOptions options) : this(options, null)
        {

        }

        public TaskQueuePool(TaskQueuePoolOptions options, ILoggerFactory loggerFactory)
        {
            _options = options;
            if (loggerFactory == null)
            {
                loggerFactory = new LoggerFactory();
                loggerFactory.AddConsole();
            }
            var type = GetType();
            Logger = loggerFactory.CreateLogger(type);
            EventId = new EventId(0, type.Name);
            LoggerFactory = loggerFactory;
        }

        public virtual object GetState()
        {
            var states = new List<string> {$"[{GetType().Name}]{_status}, Queue: {_queuedTaskData.Count}"};
            states.AddRange(this.Select(t => t.State.ToString()));
            return string.Join(Environment.NewLine, states);
        }

        public TaskQueuePoolStatus Status => _status;

        public void Enqueue(TaskData taskData)
        {
            _queuedTaskData.Enqueue(taskData);
        }
        /// <summary>
        /// 队列停止后，会清空当前所有任务数据，正在运行的任务还会继续运行，但是不会再增加新任务数据
        /// </summary>
        public virtual void Stop()
        {
            _status = TaskQueuePoolStatus.Idle;
            _queuedTaskData = new ConcurrentQueue<TaskData>();
        }

        public virtual async Task Start()
        {
            _status = TaskQueuePoolStatus.Running;

            Logger.LogInformation($"Pool [{GetType().Name}] started");
            await Task.Run(_execute);
            Logger.LogInformation($"Pool [{GetType().Name}] stopped");
        }

        private Task _execute()
        {
            while (_status == TaskQueuePoolStatus.Running && (_queuedTaskData.Any() || _currentThreadCount > 0))
            {
                if (_options.MaxThreads == 0 || _currentThreadCount < _options.MaxThreads)
                {
                    if (_queuedTaskData.TryDequeue(out var taskData))
                    {
                        var task = this.FirstOrDefault(t => t.CanExecuteData() && t.MatchedTaskData(taskData));
                        if (task != null)
                        {
                            Interlocked.Increment(ref _currentThreadCount);
                            Interlocked.Increment(ref task.CurrentThreadCount);
                            _executeTask(task, taskData);
                        }
                        else
                        {
                            _queuedTaskData.Enqueue(taskData);
                        }
                    }
                }
            }
            _status = TaskQueuePoolStatus.Idle;
            return Task.CompletedTask;
        }

        private async void _executeTask(TaskQueue task, TaskData taskData)
        {
            if (!taskData.ExecuteImmediately)
            {
                await Task.Delay(Math.Max(_options.MinInterval, task.Options.Interval));
            }
            var newTaskData = await task.ExecuteAsync(taskData);
            if (_status == TaskQueuePoolStatus.Running && newTaskData?.Any() == true)
            {
                foreach (var d in newTaskData)
                {
                    _queuedTaskData.Enqueue(d);
                }
            }
            Interlocked.Decrement(ref _currentThreadCount);
            Interlocked.Decrement(ref task.CurrentThreadCount);
        }
    }
}