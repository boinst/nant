namespace NAnt.Core.Tasks
{
    using System;
    using System.Collections.Generic;

    using NAnt.Core.Attributes;

    /// <summary>
    /// Joins tasks spawned asynchronously.
    /// </summary>
    [TaskName("join")]
    public class JoinTask : Task
    {
        /// <summary>
        /// The tasks to join to.
        /// </summary>
        private readonly List<string> tasks = new List<string>();

        /// <summary>
        /// Comma-separated list of tasks to "join".
        /// </summary>
        [TaskAttribute("task", Required = false)]
        public string TaskName
        {
            get
            {
                return string.Join(",", tasks.ToArray());
            }

            set
            {
                tasks.Clear();
                foreach (string str in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string taskToJoin = str.Trim();
                    if (taskToJoin.Length > 0)
                    {
                        tasks.Add(taskToJoin);
                    }
                }
            }
        }

        /// <summary>
        /// Set this property to "true" to join all tasks that have not yet been joined.
        /// </summary>
        [TaskAttribute("all", Required = false)]
        [BooleanValidator]
        public bool All { get; set; }

        /// <summary>Initializes the task.</summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Either a list of tasks or "all" must be provided.
            if (tasks.Count == 0 && !this.All)
                throw new BuildException("Either specify tasks to join by setting \"task\", or set \"all\" to \"true\" to join all tasks.");
        }

        /// <summary>Executes the task.</summary>
        protected override void ExecuteTask()
        {
            if (this.All)
            {
                JoinAllTasks();
            }
            else
            {
                foreach (var task in tasks)
                {
                    this.Join(task);
                }
            }
        }

        /// <summary>
        /// Join all tasks that have not yet been joined.
        /// </summary>
        private void JoinAllTasks()
        {
            if (string.IsNullOrEmpty(AsyncTask.GetFirstUnjoinedTask()))
                this.Log(Level.Info, "No Asynchronous tasks remaining to Join.");

            string taskName;
            while (!string.IsNullOrEmpty(taskName = AsyncTask.GetFirstUnjoinedTask()))
            {
                this.Join(taskName);
            }
        }

        /// <summary>
        /// Join a task with the specified name.
        /// </summary>
        /// <param name="task">
        /// The name of the task to join.
        /// </param>
        private void Join(string task)
        {
            this.Log(Level.Info, "Joining Asynchronous task \"" + task + "\".");
            AsyncTask asyncTask;
            if (!AsyncTask.TryGetTask(task, out asyncTask)) throw new BuildException("Unable to find task " + this.TaskName, this.Location);
            asyncTask.Join(this);
        }
    }
}