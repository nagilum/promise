using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Promise
{
    public Promise()
    {
        this.Promises = new List<PromiseKeeper>();
        this.Catchers = new List<PromiseCatcher>();
    }

    public bool IsRunning { get; set; }

    public object Result { get; set; }

    private List<PromiseKeeper> Promises { get; set; }

    private List<PromiseCatcher> Catchers { get; set; }

    public Promise Then<T>(Func<T> function)
    {
        var item = new PromiseKeeper
        {
            OutputType = typeof(T),
            Function = function
        };

        this.Promises.Add(item);

        return this;
    }

    public Promise Then<T, T1>(Func<T, T1> function)
    {
        var item = new PromiseKeeper
        {
            InputType = typeof(T),
            OutputType = typeof(T1),
            Function = function
        };

        this.Promises.Add(item);

        return this;
    }

    public Promise Catch(Action<Exception> function)
    {
        var item = new PromiseCatcher
        {
            ExceptionType = typeof(Exception),
            Function = function
        };

        this.Catchers.Add(item);

        return this;
    }

    public Promise Catch<T>(Action<T> function)
    {
        var item = new PromiseCatcher
        {
            ExceptionType = typeof(T),
            Function = function
        };

        this.Catchers.Add(item);

        return this;
    }

    public Promise ExecuteAsync(object initialValue = null)
    {
        if (!this.Promises.Any())
        {
            return this;
        }

        this.IsRunning = true;

        Task.Run(() =>
        {
            this.Execute(initialValue);
        });

        return this;
    }

    public Promise Execute(object initialValue = null)
    {
        if (!this.Promises.Any())
        {
            return this;
        }

        this.IsRunning = true;

        Exception UnhandledException = null;

        for (var i = 0; i < this.Promises.Count; i++)
        {
            try
            {
                object output;

                if (this.Promises[i].InputType == null)
                {
                    output = this.Promises[i].Function.DynamicInvoke();
                }
                else
                {
                    if (i == 0)
                    {
                        if (initialValue == null)
                        {
                            throw new NullReferenceException();
                        }

                        output = this.Promises[i].Function.DynamicInvoke(initialValue);
                    }
                    else
                    {
                        var input = this.Promises[i - 1].ReturnedValue;

                        if (input == null)
                        {
                            throw new NullReferenceException();
                        }

                        var inputCasted = Convert.ChangeType(input, this.Promises[i].InputType);

                        output = this.Promises[i].Function.DynamicInvoke(inputCasted);
                    }
                }

                if (this.Promises[i].OutputType == typeof(Promise))
                {
                    var promise = (Promise)output;

                    while (promise.IsRunning)
                    {
                        Task.Delay(1);
                    }

                    output = promise.Result;
                }

                if (output != null)
                {
                    this.Promises[i].ReturnedValue = output;
                }
            }
            catch (Exception ex)
            {
                UnhandledException = this.HandleException(ex);
                break;
            }
        }

        this.Result = this.Promises.Last().ReturnedValue;
        this.IsRunning = false;

        if (UnhandledException != null)
        {
            throw UnhandledException;
        }

        return this;
    }

    public T CastTo<T>()
    {
        if (this.Result == null)
        {
            var uex = this.HandleException(new NullReferenceException());

            if (uex != null)
            {
                throw uex;
            }
        }

        try
        {
            return (T)this.Result;
        }
        catch (Exception ex)
        {
            var uex = this.HandleException(ex);

            if (uex != null)
            {
                throw uex;
            }

            return default(T);
        }
    }

    private Exception HandleException(Exception ex)
    {
        var type = ex.GetType();

        foreach (var catcher in this.Catchers)
        {
            if (catcher.ExceptionType != type)
            {
                continue;
            }

            catcher.Function.DynamicInvoke(ex);
            return null;
        }

        type = typeof(Exception);

        foreach (var catcher in this.Catchers)
        {
            if (catcher.ExceptionType != type)
            {
                continue;
            }

            catcher.Function.DynamicInvoke(ex);
            return null;
        }

        return ex;
    }

    private class PromiseKeeper
    {
        public object ReturnedValue { get; set; }

        public Type InputType { get; set; }

        public Type OutputType { get; set; }

        public Delegate Function { get; set; }
    }

    private class PromiseCatcher
    {
        public Type ExceptionType { get; set; }

        public Delegate Function { get; set; }
    }
}