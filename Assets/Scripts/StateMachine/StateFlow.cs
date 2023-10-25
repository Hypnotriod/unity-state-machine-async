using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace StateMachine
{
    public class StateFlow : IStateFlowHandler
    {
        public bool IsAcive
        {
            get { return _isActive; }
        }

        private bool _isActive = true;
        private readonly List<Queue<Func<CancellationToken, UniTask>>> _tasksQueues;
        private readonly Action _completeAction;
        private readonly Action<IStateFlowHandler> _beginAction;
        private readonly Dictionary<string, Action> _cancellationHandlers = new();
        private Action _suspendedCancellationAction;
        private CancellationTokenSource _flowCts;

        public StateFlow(List<Queue<Func<CancellationToken, UniTask>>> tasksQueues)
        {
            _tasksQueues = tasksQueues;
        }

        public StateFlow(List<Queue<Func<CancellationToken, UniTask>>> tasksQueues, Action<IStateFlowHandler> beginAction, Action completeAction)
        {
            _tasksQueues = tasksQueues;
            _beginAction = beginAction;
            _completeAction = completeAction;
        }

        public StateFlow(Queue<Func<CancellationToken, UniTask>> queue, Action<IStateFlowHandler> beginAction, Action completeAction)
        {
            _tasksQueues = new() { queue };
            _beginAction = beginAction;
            _completeAction = completeAction;
        }

        public StateFlow(List<Queue<Func<CancellationToken, UniTask>>> tasksQueues, Action<IStateFlowHandler> beginAction)
        {
            _tasksQueues = tasksQueues;
            _beginAction = beginAction;
        }

        public StateFlow(Queue<Func<CancellationToken, UniTask>> queue, Action<IStateFlowHandler> beginAction)
        {
            _tasksQueues = new() { queue };
            _beginAction = beginAction;
        }

        public StateFlow(List<Queue<Func<CancellationToken, UniTask>>> tasksQueues, Action completeAction)
        {
            _tasksQueues = tasksQueues;
            _completeAction = completeAction;
        }

        public StateFlow(Queue<Func<CancellationToken, UniTask>> queue, Action completeAction)
        {
            _tasksQueues = new() { queue };
            _completeAction = completeAction;
        }

        public StateFlow(Action<IStateFlowHandler> beginAction)
        {
            _beginAction = beginAction;
        }

        public void Suspend()
        {
            _isActive = false;
        }

        public void Resume()
        {
            _isActive = true;
            if (_suspendedCancellationAction != null)
            {
                var action = _suspendedCancellationAction;
                _suspendedCancellationAction = null;
                action();
            }
        }

        public void Emit(string signal)
        {
            _cancellationHandlers.TryGetValue(signal, out var action);
            if (action != null)
            {
                Cancel();
                if (!_isActive)
                {
                    _suspendedCancellationAction = action;
                }
                else
                {
                    action();
                }
            }
        }

        public void RegistedCencellationSignalHandler(Action action, params string[] signals)
        {
            foreach (string signal in signals)
            {
                _cancellationHandlers.Add(signal, action);
            }
        }

        public void Cancel()
        {
            _flowCts?.Cancel();
            Drain();
        }

        public async void Lauch()
        {
            _flowCts = new CancellationTokenSource();
            _beginAction?.Invoke(this);
            var tasks = new List<UniTask>();
            if (_tasksQueues != null)
            {
                foreach (var tasksQueue in _tasksQueues)
                    tasks.Add(DequeueTasks(tasksQueue, _flowCts.Token));
            }
            await UniTask.WhenAll(tasks);
            if (_flowCts != null && !_flowCts.IsCancellationRequested)
                _completeAction?.Invoke();
            if (_completeAction != null) // waiting for signals to be emitted if no complete action provided
                Drain();
        }

        private async UniTask LauchNested(CancellationToken token)
        {
            var tasks = new List<UniTask>();
            if (_tasksQueues != null)
            {
                foreach (var tasksQueue in _tasksQueues)
                    tasks.Add(DequeueTasks(tasksQueue, token));
            }
            await UniTask.WhenAll(tasks);
        }

        private void Drain()
        {
            _flowCts = null;
            _suspendedCancellationAction = null;
            _cancellationHandlers.Clear();
        }

        private async UniTask DequeueTasks(Queue<Func<CancellationToken, UniTask>> tasks, CancellationToken token)
        {
            while (tasks.Count > 0)
            {
                var task = tasks.Dequeue();
                await task(token);
                if (token.IsCancellationRequested) { return; }
                if (!_isActive)
                    await UniTask.WaitUntil(() => _isActive, cancellationToken: token).SuppressCancellationThrow();
            }
        }

        public static List<Queue<Func<CancellationToken, UniTask>>> InParallel(params Queue<Func<CancellationToken, UniTask>>[] list)
        {
            return new List<Queue<Func<CancellationToken, UniTask>>>(list);
        }

        public static List<Queue<Func<CancellationToken, UniTask>>> InParallel(params Func<CancellationToken, UniTask>[] list)
        {
            var result = new List<Queue<Func<CancellationToken, UniTask>>>();
            foreach (var handler in list)
            {
                var queue = new Queue<Func<CancellationToken, UniTask>>();
                queue.Enqueue(handler);
                result.Add(queue);
            }
            return result;
        }

        public static Queue<Func<CancellationToken, UniTask>> InSequence(params Func<CancellationToken, UniTask>[] list)
        {
            return new Queue<Func<CancellationToken, UniTask>>(list);
        }

        public static UniTask InParallelNested(CancellationToken token, params Queue<Func<CancellationToken, UniTask>>[] list)
        {
            return InParallelNested(InParallel(list), token);
        }

        public static UniTask InParallelNested(CancellationToken token, params Func<CancellationToken, UniTask>[] list)
        {
            return InParallelNested(InParallel(list), token);
        }

        private static UniTask InParallelNested(List<Queue<Func<CancellationToken, UniTask>>> list, CancellationToken token)
        {
            return new StateFlow(list).LauchNested(token);
        }

        public static UniTask InSync(params Action[] actions)
        {
            foreach (var action in actions)
                action();
            return UniTask.CompletedTask;
        }
    }
}
