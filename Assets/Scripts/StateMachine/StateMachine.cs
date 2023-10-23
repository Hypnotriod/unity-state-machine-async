public class StateMachine
{
    private State _state;

    public void NextState(State state)
    {
        _state?.Cancel();
        _state = state;
        _state.Lauch();
    }

    public void Emit(string signal)
    {
        _state?.Emit(signal);
    }
}
