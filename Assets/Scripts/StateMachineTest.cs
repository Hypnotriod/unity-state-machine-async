using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StateMachineTest : MonoBehaviour
{
    public static readonly string SPACE_KEY_PRESSED = "SPACE_KEY_PRESSED";
    public static readonly string Q_KEY_PRESSED = "Q_KEY_PRESSED";

    private readonly StateMachine _stateMachine = new();

    void Start()
    {
        NextState(Initial());
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

    private State Initial() => new(InParallel(
        InSequence(
            t => Delay(4000, t)
        ),
        InSequence(
            t => Delay(5000, t),
            t => InParallelNested(t,
                t => Delay(2000, t),
                t => Delay(3000, t)
            )
        )
    ), stateHandler =>
    {
        Debug.LogFormat("State Machine: Started at {0}", Time.realtimeSinceStartup);
        stateHandler.RegistedSignalHandler(() =>
        {
            Debug.LogFormat("State Machine: Cancelled at {0}", Time.realtimeSinceStartup);
        }, SPACE_KEY_PRESSED);
    }, () =>
    {
        NextState(Next());
    });

    private State Next() => new(InParallel(
       InSequence(
           t => Delay(1000, t)
       ),
       InSequence(
           t => Delay(2000, t),
           t => InParallelNested(t,
               t => Delay(3000, t),
               t => Delay(4000, t)
           ),
           t => InParallelNested(t,
               t => Delay(5000, t),
               t => Delay(6000, t)
           )
       )
   ), stateHandler =>
   {
       Debug.LogFormat("State Machine: Proceeded at {0}", Time.realtimeSinceStartup);
       stateHandler.RegistedSignalHandler(() =>
       {
           Debug.LogFormat("State Machine: Cancelled at {0}", Time.realtimeSinceStartup);
       }, Q_KEY_PRESSED);
   }, () =>
   {
       Debug.LogFormat("State Machine: Completed at {0}", Time.realtimeSinceStartup);
   });

    protected UniTask Delay(int delayMilliseconds, CancellationToken token)
    {
        Debug.LogFormat("State Machine: Delay {0} milliseconds", delayMilliseconds);
        return UniTask.Delay(delayMilliseconds, cancellationToken: token).SuppressCancellationThrow();
    }

    protected void SyncAction()
    {
        Debug.Log("State Machine: SyncAction");
    }

    #region Helper functions
    protected List<Queue<Func<CancellationToken, UniTask>>> InParallel(params Queue<Func<CancellationToken, UniTask>>[] list) => State.InParallel(list);
    protected List<Queue<Func<CancellationToken, UniTask>>> InParallel(params Func<CancellationToken, UniTask>[] list) => State.InParallel(list);
    protected Queue<Func<CancellationToken, UniTask>> InSequence(params Func<CancellationToken, UniTask>[] list) => State.InSequence(list);
    protected UniTask InParallelNested(CancellationToken token, params Queue<Func<CancellationToken, UniTask>>[] list) => State.InParallelNested(token, list);
    protected UniTask InParallelNested(CancellationToken token, params Func<CancellationToken, UniTask>[] list) => State.InParallelNested(token, list);
    protected void NextState(State state) => _stateMachine.NextState(state);
    #endregion
}
