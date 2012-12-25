namespace NAnt.Core.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using NAnt.Core.Attributes;

    /// <summary>
    /// Executes the good stuff within this container asynchronously,
    /// caching any log messages to be retrieved later by a call to
    /// <see cref="JoinTask"/>.
    /// </summary>
    [TaskName("async")]
    public class AsyncTask : TaskContainer
    {
        /// <summary>
        /// lock object to guard access to <see cref="asyncTasks"/>.
        /// </summary>
        private static readonly object taskRegisterSyncObject = new object();

        /// <summary>
        /// Async tasks that have been started. We use this register to "join" them back together.
        /// </summary>
        private static Dictionary<string, AsyncTask> asyncTasks = new Dictionary<string, AsyncTask>();

        /// <summary>
        /// Cached log events that will be logged when the Target finishes executing.
        /// </summary>
        /// <remarks>
        /// Added for Parallel NAnt.
        /// Appended to by <see cref="IParent.Log"/>.
        /// </remarks>
        private readonly List<BuildEventArgs> logEvents = new List<BuildEventArgs>();

        /// <summary>
        /// lock object to guard access to <see cref="joined"/>.
        /// </summary>
        private readonly object joinSyncObject = new object();

        /// <summary>
        /// This task has been joined.
        /// </summary>
        private bool joined;

        /// <summary>
        /// An exception thrown by a child task
        /// </summary>
        private Exception caughtException;

        /// <summary>
        /// The thread we do the background work on.
        /// </summary>
        private Thread worker;

        /// <summary>
        /// Names the task so that you can use <see cref="JoinTask"/> later.
        /// </summary>
        [TaskAttribute("taskname", Required = true)]
        public string TaskName
        {
            get;
            set;
        }

        public override void Log(BuildEventArgs buildEvent)
        {
            logEvents.Add(buildEvent);
        }

        protected override void ExecuteTask()
        {
            this.RegisterTask();
            base.ExecuteTask();
        }

        /// <summary>
        /// Register the task with <see cref="asyncTasks"/> so that it may be retrieved later.
        /// </summary>
        private void RegisterTask()
        {
            lock (taskRegisterSyncObject)
            {
                if (asyncTasks.ContainsKey(TaskName))
                    throw new BuildException("An ASyncTask has already been started with the name " + TaskName, Location);

                asyncTasks.Add(TaskName, this);
            }
        }

        protected override void ExecuteChildTasks()
        {
            base.Log(new BuildEventArgs(this) { MessageLevel = Level.Info, Message = "Forking Asynchronous task \"" + TaskName + "\"." });
            worker = new Thread(
                a =>
                    {
                        try
                        {
                            base.ExecuteChildTasks();
                        }
                        catch (Exception ex)
                        {
                            this.caughtException = ex;
                        }
                    });
            worker.Start();
        }

        public void Join(JoinTask joinTask)
        {
            worker.Join();

            lock (this.joinSyncObject)
            {
                // If this task was already joined, do nothing.
                if (joined) return;

                // Block double-joining.
                joined = true;
            }

            // Log any collected messages.
            foreach (var logEvent in logEvents)
            {
                joinTask.Log(logEvent);
            }

            // Throw any collected exception.
            if (caughtException != null) throw caughtException;
        }

        public static bool TryGetTask(string name, out AsyncTask task)
        {
            lock (taskRegisterSyncObject)
                return asyncTasks.TryGetValue(name, out task);
        }

        public static string GetFirstUnjoinedTask()
        {
            lock (taskRegisterSyncObject)
            {
                foreach (var asyncTask in asyncTasks.Values)
                {
                    if (!asyncTask.joined) return asyncTask.TaskName;
                }

                return null;
            }
        }
    }
}
