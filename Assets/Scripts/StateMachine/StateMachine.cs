public class StateMachine
{
    private State _state;

    public void NextState(State state)
    {
        _state?.Cancel();
        _state = state;
        _state.Lauch();
    }

    public void Signal(string signal)
    {
        _state?.Signal(signal);
    }
}
