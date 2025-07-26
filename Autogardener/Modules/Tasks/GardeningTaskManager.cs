using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DalamudBasics.Configuration;
using DalamudBasics.Logging;
using ECommons.Automation.NeoTaskManager;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.Throttlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks
{
    public class GardeningTaskManager
    {
        private Stack<LinkedList<TaskManagerTask>> taskListStack = new();
        private LinkedList<TaskManagerTask> currentTaskList => GetTopStackOrInitialize();
        private HashSet<Guid> stackIds = new();
        public IGameObject? currentTarget = null;


        private readonly IClientState clientState;
        private readonly IFramework framework;
        private readonly IChatGui chatGui;
        private readonly ILogService logService;
        private readonly IConfigurationService<Configuration> configService;
        private readonly TaskManager taskManager;

        public GardeningTaskManager(IClientState clientState, IFramework framework, IChatGui chatGui, ILogService logService,
            IConfigurationService<Configuration> configService)
        {
            this.clientState = clientState;
            this.framework = framework;
            this.chatGui = chatGui;
            this.logService = logService;
            this.configService = configService;
            taskManager = new TaskManager();
        }

        public bool IsBusy()
        {
            return taskManager.IsBusy;
        }

        public void Abort()
        {
            taskManager.Abort();
            taskListStack.Clear();
        }

        public string GetCurrentTaskName()
        {
            return taskManager.CurrentTask?.Name ?? "None";
        }

        public void StartProcessingQueuedTasks()
        {
            PopAndInsertTaskStack(Guid.NewGuid());
        }

        public void Enqueue(Func<bool> function, string name)
        {
            logService.Info($"Enqueued function: {name}");
            Func<bool> task = () => 
            {
                logService.Warning($"Running {name}");
                return function();
            };
            currentTaskList.Add(new DelayTask(configService.GetConfiguration().StepDelayInMs));
            currentTaskList.Add(new TaskManagerTask(task, name, DefConfig));
        }

        public void Enqueue(Action action, string name)
        {
            logService.Info($"Enqueued action: {name}");
            Action task = () =>
            {
                logService.Warning($"Running {name}");
                action();
            };
            currentTaskList.Add(new DelayTask(configService.GetConfiguration().StepDelayInMs));
            currentTaskList.Add(new TaskManagerTask(action, name, DefConfig));
        }

        public void EnqueueDelayMs(int ms)
        {
            logService.Info($"Enqueueing delay of {ms} ms");
            Func<bool> throttle = () =>
            {
                
                var key = "Generic throttle";
                if (!EzThrottler.ThrottleNames.Contains(key)){
                    EzThrottler.Throttle(key, ms, true);
                    return false;
                }

                return EzThrottler.Check(key);
            };

            currentTaskList.Add(new TaskManagerTask(throttle, $"Delay of {ms} milliseconds"));
        }

        public void EnqueueSuperTask(Action action, string name)
        {
            EnqueueSuperTask(() => { action(); return true; }, name);
        }

        // Tasks that can insert new tasks
        public void EnqueueSuperTask(Func<bool> function, string name)
        {
            var newStackId = Guid.NewGuid();
            logService.Info($"Enqueued super function: {name}");
            Func<bool> superTask = () =>
            {
                TryIncreaseTaskListStack(newStackId);
                var result = function();
                if (result == true)
                {
                    PopAndInsertTaskStack(newStackId);
                }
                return result;
            };

            Enqueue(superTask, name);
        }

        public void Insert(Func<bool> function, string name)
        {
            currentTaskList.AddFirst(new TaskManagerTask(function, name, DefConfig));
        }

        private bool TryIncreaseTaskListStack(Guid newStackId)
        {
            if (stackIds.Contains(newStackId))
            {
                return false;
            }
            taskListStack.Push(new LinkedList<TaskManagerTask>());
            stackIds.Add(newStackId);
            logService.Info($"Stack increased. Current stack size: {taskListStack.Count}. Id: {newStackId}");

            return true;
        }

        private LinkedList<TaskManagerTask> GetTopStackOrInitialize()
        {
            if (taskListStack.Count == 0)
            {
                taskListStack.Push(new LinkedList<TaskManagerTask>());
            }

            return taskListStack.Peek();
        }
        private void PopAndInsertTaskStack(Guid stackId)
        {
            logService.Info("Stack inserted into taskManager");
            taskManager.InsertMulti(taskListStack.Pop().ToArray());
            stackIds.Remove(stackId);
        }

        private static TaskManagerConfiguration DefConfig = new TaskManagerConfiguration()
        {
            ShowDebug = true,
            ShowError = true,
            TimeLimitMS = 10000,
            AbortOnTimeout = true
        };
    }
}
