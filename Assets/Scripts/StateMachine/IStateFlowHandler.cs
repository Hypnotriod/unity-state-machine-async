using System;

public interface IStateFlowHandler
{
    bool IsAcive { get; }
    void RegistedCancellationSignalHandler(Action cancellationAction, params string[] signals);
}
