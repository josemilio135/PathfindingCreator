using System;

public class FuncPredicate : IPredicate
{
    public Func<bool> _func;
    public FuncPredicate(Func<bool> func)
    {
        _func = func;
    }

    public bool Evaluate()
    {
        return _func.Invoke();
    }
}