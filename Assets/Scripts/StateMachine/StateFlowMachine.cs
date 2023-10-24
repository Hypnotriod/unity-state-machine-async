namespace StateMachine
{
    public class StateFlowMachine
    {
        private StateFlow _state;

        public void NextState(StateFlow state)
        {
            _state?.Cancel();
            _state = state;
            _state.Lauch();
        }

        public void Emit(string signal)
        {
            _state?.Emit(signal);
        }

        public void Suspend()
        {
            _state?.Suspend();
        }

        public void Resume()
        {
            _state?.Resume();
        }
    }
}

