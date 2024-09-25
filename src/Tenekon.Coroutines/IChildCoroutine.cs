namespace Tenekon.Coroutines;

internal interface IChildCoroutine
{
    void ActOnCoroutine(in CoroutineContext context);
}
