using Cysharp.Threading.Tasks;
using StateMachine;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StateMachineTest : MonoBehaviour
{
    public static readonly string SPACE_KEY_PRESSED = "SPACE_KEY_PRESSED";
    public static readonly string Q_KEY_PRESSED = "Q_KEY_PRESSED";

    private readonly StateFlowMachine _stateMachine = new();

    void Start()
    {
        NextState(InitialState());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space key pressed!");
            _stateMachine.Emit(SPACE_KEY_PRESSED);
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Q key pressed!");
            _stateMachine.Emit(Q_KEY_PRESSED);
        }
    }

    private StateFlow InitialState() => new(
    stateHandler =>
    {
        Debug.LogFormat("State Machine: Started at {0}", Time.realtimeSinceStartup);
        stateHandler.RegistedCencellationSignalHandler(() =>
        {
            Debug.LogFormat("State Machine: Cancelled at {0}", Time.realtimeSinceStartup);
        }, SPACE_KEY_PRESSED);
    },
    InParallel(
        InSequence(
            t => Delay(t, 4000),
            _ => InSync(SyncAction, () => SyncActionWithMessage("test"))
        ),
        InSequence(
            t => Delay(t, 5000),
            t => InParallelNested(t,
                t => Delay(t, 2000),
                t => Delay(t, 3000)
            )
        )
    ), () =>
    {
        NextState(SecondState());
    });

    private StateFlow SecondState() => new(
    stateHandler =>
    {
        Debug.LogFormat("State Machine: Proceeded to SecondState at {0}", Time.realtimeSinceStartup);
        stateHandler.RegistedCencellationSignalHandler(() =>
        {
            Debug.LogFormat("State Machine: Cancelled at {0}", Time.realtimeSinceStartup);
        }, Q_KEY_PRESSED);
    },
    InParallel(
        InSequence(
            t => Delay(t, 1000)
        ),
        InSequence(
            t => Delay(t, 2000),
            t => InParallelNested(t,
                t => Delay(t, 3000),
                t => Delay(t, 4000)
            ),
            t => InParallelNested(t,
                t => Delay(t, 5000),
                t => Delay(t, 6000)
            )
        )
    ), () =>
    {
        NextState(SimpleState());
    });

    private StateFlow SimpleState() => new(
    stateHandler =>
    {
        Debug.LogFormat("State Machine: Proceeded to SimpleState at {0}", Time.realtimeSinceStartup);
    }, async t =>
    {
        await Delay(t, 1500);
    }, () =>
    {
        Debug.LogFormat("State Machine: Completed at {0}", Time.realtimeSinceStartup);
    });

    protected UniTask Delay(CancellationToken token, int delayMilliseconds)
    {
        Debug.LogFormat("State Machine: Delay {0} milliseconds", delayMilliseconds);
        return UniTask.Delay(delayMilliseconds, cancellationToken: token).SuppressCancellationThrow();
    }

    protected void SyncAction()
    {
        Debug.Log("State Machine: SyncAction");
    }

    protected void SyncActionWithMessage(string message)
    {
        Debug.LogFormat("State Machine: SyncActionWithMessage: {0}", message);
    }

    #region Helper functions
    protected List<Queue<Func<CancellationToken, UniTask>>> InParallel(params Queue<Func<CancellationToken, UniTask>>[] list) => StateFlow.InParallel(list);
    protected List<Queue<Func<CancellationToken, UniTask>>> InParallel(params Func<CancellationToken, UniTask>[] list) => StateFlow.InParallel(list);
    protected Queue<Func<CancellationToken, UniTask>> InSequence(params Func<CancellationToken, UniTask>[] list) => StateFlow.InSequence(list);
    protected UniTask InParallelNested(CancellationToken token, params Queue<Func<CancellationToken, UniTask>>[] list) => StateFlow.InParallelNested(token, list);
    protected UniTask InParallelNested(CancellationToken token, params Func<CancellationToken, UniTask>[] list) => StateFlow.InParallelNested(token, list);
    protected void NextState(StateFlow state) => _stateMachine.NextState(state);
    protected UniTask InSync(params Action[] actions) => StateFlow.InSync(actions);
    #endregion
}
