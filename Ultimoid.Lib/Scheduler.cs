using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Ultimoid.Lib {
    public class Scheduler {
        public struct SchedulerTask {
            public enum TaskType {
                OneShot,
                PeriodicLimited,
                PeriodicForever
            }

            public TimeSpan Delay;
            public TimeSpan Period;
            public Action Action;
            public TaskType Type;
            public int IterationsRemaining;

            private SchedulerTask(TimeSpan delay, TimeSpan period, Action action, TaskType type,
                int iterationsRemaining) {
                Delay = delay;
                Period = period;
                Action = action;
                Type = type;
                IterationsRemaining = iterationsRemaining;

                Debug.Assert(Type != TaskType.PeriodicForever || IterationsRemaining == -1,
                    "Type == TaskType.PeriodicForever && IterationsRemaining == -1");
                Debug.Assert(type != TaskType.PeriodicLimited || IterationsRemaining > 0,
                    "type == TaskType.PeriodicLimited && IterationsRemaining > 0");
            }

            public SchedulerTask WithChangedDelay(TimeSpan delay) {
                return new SchedulerTask(delay, Period, Action, Type, IterationsRemaining);
            }

            public SchedulerTask WithChangedDelayAndIterations(TimeSpan delay, int iterations) {
                return new SchedulerTask(delay, Period, Action, Type, iterations);
            }

            public static SchedulerTask OneShotTask(TimeSpan delay, Action action) {
                return new SchedulerTask(delay, TimeSpan.Zero, action, TaskType.OneShot, 1);
            }

            public static SchedulerTask PeriodicLimitedTask(TimeSpan delay, TimeSpan period, int iterations,
                Action action) {
                return new SchedulerTask(delay, period, action, TaskType.PeriodicLimited, iterations);
            }

            public static SchedulerTask PeriodicForeverTask(TimeSpan delay, TimeSpan period, Action action) {
                return new SchedulerTask(delay, period, action, TaskType.PeriodicForever, -1);
            }
        }

        private List<SchedulerTask> _tasks = new List<SchedulerTask>();
        private TimeSpan _elapsedTime = TimeSpan.Zero;

        public void RunIn(TimeSpan delay, Action action) {
            // TODO: possibly record a stacktrace here together with the task,
            //       so that it can be used later when diagnosing a failed run task
            _tasks.Add(SchedulerTask.OneShotTask(delay, action));
        }

        public void RunPeriodically(TimeSpan period, Action action) {
            RunPeriodically(TimeSpan.Zero, period, action);
        }

        public void RunPeriodically(TimeSpan delay, TimeSpan period, Action action) {
            _tasks.Add(SchedulerTask.PeriodicForeverTask(delay, period, action));
        }

        public void RunPeriodicallyLimited(TimeSpan period, int iterations, Action action) {
            RunPeriodicallyLimited(TimeSpan.Zero, period, iterations, action);
        }

        public void RunPeriodicallyLimited(TimeSpan delay, TimeSpan period, int iterations, Action action) {
            _tasks.Add(SchedulerTask.PeriodicLimitedTask(delay, period, iterations, action));
        }

        public void Update(TimeSpan deltaTime) {
            _elapsedTime = _elapsedTime.Add(deltaTime);

            var newTasks = new List<SchedulerTask>();
            var toExecute = new List<Action>();

            foreach (var task in _tasks) {
                TimeSpan newTime = task.Delay.Subtract(deltaTime);

                bool shouldExecute = newTime.TotalMilliseconds <= 0;
                if (shouldExecute) {
                    toExecute.Add(task.Action);

                    switch (task.Type) {
                        case SchedulerTask.TaskType.OneShot:
                            break;

                        case SchedulerTask.TaskType.PeriodicLimited:
                            if (task.IterationsRemaining > 1) {
                                newTasks.Add(task.WithChangedDelayAndIterations(task.Period,
                                    task.IterationsRemaining - 1));
                            }
                            break;

                        case SchedulerTask.TaskType.PeriodicForever:
                            newTasks.Add(task.WithChangedDelay(task.Period));
                            break;
                        default:

                            throw new InvalidOperationException($"Invalid task type ${task.Type}.");
                    }
                } else {
                    newTasks.Add(task.WithChangedDelay(newTime));
                }


                //else {
                //    newTasks.Add(new SchedulerTask(newTime, task.Action));
                //}
            }

            // ************************************************************
            // This has to be done before running the actions, so that they
            // can enqueue new tasks into the new queue
            // ************************************************************
            _tasks = newTasks;

            var exceptions = new List<Exception>();

            // TODO: carry over the initial state
            foreach (var action in toExecute) {
                try {
                    action();
                } catch (Exception e) {
                    exceptions.Add(e);
                    Console.Error.WriteLine(e);
                }
            }

            // TODO: log exceptions properly
        }
    }
}