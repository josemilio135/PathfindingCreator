using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Global coroutine executor for non-MonoBehaviour classes.
/// Automatically creates itself when first used.
/// </summary>
public sealed class CoroutineManager : MonoBehaviour
{
    static CoroutineManager _instance;

    /// <summary>
    /// Singleton instance.
    /// Creates the manager automatically if it does not exist.
    /// </summary>
    static CoroutineManager Instance
    {
        get
        {
            if (_instance == null || !_instance)
            {
                var go = new GameObject("[CoroutineManager]");

                DontDestroyOnLoad(go);

                _instance = go.AddComponent<CoroutineManager>();
            }

            return _instance;
        }
    }

    void Awake()
    {
        // Prevent duplicate instances.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Starts a coroutine globally.
    /// </summary>
    public static Coroutine Start(IEnumerator coroutine)
    {
        return Instance.StartCoroutine(coroutine);
    }

    /// <summary>
    /// Stops a specific coroutine.
    /// </summary>
    public static void Stop(Coroutine coroutine)
    {
        if (coroutine != null && _instance != null)
            _instance.StopCoroutine(coroutine);
    }

    /// <summary>
    /// Stops all active coroutines running in this manager.
    /// </summary>
    public static void StopAll()
    {
        if (_instance != null)
            _instance.StopAllCoroutines();
    }

    /// <summary>
    /// Executes the callback when the predicate becomes true.
    /// </summary>
    public static Coroutine ExecuteWhen(Func<bool> predicate, Action callback)
    {
        return Start(ExecuteWhenRoutine(predicate, callback));
    }

    /// <summary>
    /// Internal ExecuteWhen coroutine.
    /// </summary>
    static IEnumerator ExecuteWhenRoutine(Func<bool> predicate, Action callback)
    {
        yield return new WaitUntil(predicate);
        callback?.Invoke();
    }
}
