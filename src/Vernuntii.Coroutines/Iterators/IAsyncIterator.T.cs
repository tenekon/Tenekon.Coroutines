﻿namespace Vernuntii.Coroutines.Iterators;

public interface IAsyncIterator<TResult>
{
    bool IsCloneable { get; }

    /// <summary>
    /// The value yielded when the underlying coroutine suspends at a compatible suspension point.
    /// A non-compatible suspension point is defined as awaiting a task-like object that is not a coroutine yielder.
    /// </summary>
    /// <returns></returns>
    object Current { get; }

    /// <summary>
    /// <see cref="MoveNextAsync"/> will suspend at the next compatible suspension point in the underlying coroutine.
    /// A non-compatible suspension point is defined as awaiting a non-coroutine task-like object in the underlying coroutine.
    /// </summary>
    /// <returns></returns>
    ValueTask<bool> MoveNextAsync();

    void YieldReturn<TYieldResult>(TYieldResult result);

    void Return(TResult result);

    /// <summary>
    /// The <see cref="Throw(Exception)"/> method, when called, can be seen as if a throw exception; statement is inserted in the generator's body at the current suspended position,
    /// where exception is the exception passed to the  <see cref="Throw(Exception)"/> method. Therefore, in a typical flow, calling  <see cref="Throw(Exception)"/> will cause the generator to throw.
    /// However, if the yield expression is wrapped in a try...catch block, the error may be caught and control flow can either resume after error handling, or exit gracefully.
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="InvalidOperationException"></exception>
    void Throw(Exception e);

    /// <summary>
    /// Once <see cref="MoveNextAsync"/> returns <see langword="false"/>, 
    /// <see cref="GetResult"/> will return either a result of type <typeparamref name="TResult"/> or the exception generated by the underlying coroutine.
    /// The behavior of <see cref="GetResult"/> is identical to <see cref="ValueTaskAwaiter{TReturnResult}.GetResult"/>; it cannot be called a second time.
    /// </summary>
    /// <returns></returns>
    TResult GetResult();

    /// <summary>
    /// Resumes execution from the last suspension point, breaking the iterator semantics by transferring full responsibility back to the coroutine.
    /// The behavior of <see cref="GetResultAsync"/> is identical to awaiting a <see cref="ValueTask"/>; it cannot be called a second time.
    /// </summary>
    /// <returns></returns>
    Coroutine<TResult> GetResultAsync();

    IAsyncIterator Clone();
}
