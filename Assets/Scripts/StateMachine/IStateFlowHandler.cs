using System;

public interface IStateFlowHandler
{
    bool IsAcive { get; }
    void RegistedCencellationSignalHandler(Action cancellationAction, params string[] signals);
}
