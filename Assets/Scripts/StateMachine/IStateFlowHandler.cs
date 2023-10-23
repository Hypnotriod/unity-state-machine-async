using System;

public interface IStateFlowHandler
{
    bool IsAcive { get; }
    void RegistedSignalHandler(Action cancellationAction, params string[] signals);
}
