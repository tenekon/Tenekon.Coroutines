namespace Tenekon.Coroutines;

internal interface ICoroutineResultStateMachineHolder
{
    void IncrementBackgroundTasks() { }

    void RegisterCriticalBackgroundTaskAndNotifyOnCompletion<TAwaiter>(ref TAwaiter forkAwaiter, Action forkCompleted) where TAwaiter : ICriticalNotifyCompletion;

    void DecrementBackgroundTasks() { }
}
