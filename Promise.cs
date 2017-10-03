using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// 
/// </summary>
public class Promise {
    /// <summary>
    /// 
    /// </summary>
    public Promise() {
        this.Promises = new List<PromiseKeeper>();
        this.Catchers = new List<PromiseCatcher>();
    }

    /// <summary>
    /// 
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public object Result { get; set; }

    /// <summary>
    /// 
    /// </summary>
    private List<PromiseKeeper> Promises { get; set; }

    /// <summary>
    /// 
    /// </summary>
    private List<PromiseCatcher> Catchers { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="function"></param>
    /// <returns></returns>
    public Promise Then<T>(Func<T> function) {
        this.Promises.Add(
            new PromiseKeeper {
                OutputType = typeof(T),
                Function = function
            });

        return this;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <param name="function"></param>
    /// <returns></returns>
    public Promise Then<T, T1>(Func<T, T1> function) {
        this.Promises.Add(
            new PromiseKeeper {
                InputType = typeof(T),
                OutputType = typeof(T1),
                Function = function
            });

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="function"></param>
    /// <returns></returns>
    public Promise Catch(Action<Exception> function) {
        this.Catchers.Add(
            new PromiseCatcher {
                ExceptionType = typeof(Exception),
                Function = function
            });

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="function"></param>
    /// <returns></returns>
    public Promise Catch<T>(Action<T> function) {
        this.Catchers.Add(
            new PromiseCatcher {
                ExceptionType = typeof(T),
                Function = function
            });

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="initialValue"></param>
    /// <returns></returns>
    public Promise ExecuteAsync(object initialValue = null) {
        if (!this.Promises.Any()) {
            return this;
        }

        this.IsRunning = true;

        Task.Run(() => {
            this.Execute(initialValue);
        });

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="initialValue"></param>
    /// <returns></returns>
    public Promise Execute(object initialValue = null) {
        if (!this.Promises.Any()) {
            return this;
        }

        this.IsRunning = true;

        Exception UnhandledException = null;

        for (var i = 0; i < this.Promises.Count; i++) {
            try {
                object output;

                if (this.Promises[i].InputType == null) {
                    output = this.Promises[i].Function.DynamicInvoke();
                }
                else {
                    if (i == 0) {
                        if (initialValue == null) {
                            throw new NullReferenceException();
                        }

                        output = this.Promises[i].Function.DynamicInvoke(initialValue);
                    }
                    else {
                        var input = this.Promises[i - 1].ReturnedValue;

                        if (input == null) {
                            throw new NullReferenceException();
                        }

                        var inputCasted = Convert.ChangeType(input, this.Promises[i].InputType);

                        output = this.Promises[i].Function.DynamicInvoke(inputCasted);
                    }
                }

                if (this.Promises[i].OutputType == typeof(Promise)) {
                    var promise = (Promise) output;

                    while (promise.IsRunning) {
                        Task.Delay(1);
                    }

                    output = promise.Result;
                }

                if (output != null) {
                    this.Promises[i].ReturnedValue = output;
                }
            }
            catch (Exception ex) {
                UnhandledException = this.HandleException(ex);
                break;
            }
        }

        this.Result = this.Promises.Last().ReturnedValue;
        this.IsRunning = false;

        if (UnhandledException != null) {
            throw UnhandledException;
        }

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T CastTo<T>() {
        if (this.Result == null) {
            var uex = this.HandleException(new NullReferenceException());

            if (uex != null) {
                throw uex;
            }
        }

        try {
            return (T) this.Result;
        }
        catch (Exception ex) {
            var uex = this.HandleException(ex);

            if (uex != null) {
                throw uex;
            }

            return default(T);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Wait() {
        while (this.IsRunning) {
            Task.Delay(1);
        }

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    private Exception HandleException(Exception ex) {
        var type = ex.GetType();

        foreach (var catcher in this.Catchers) {
            if (catcher.ExceptionType != type) {
                continue;
            }

            catcher.Function.DynamicInvoke(ex);
            return null;
        }

        if (type == typeof(Exception)) {
            return ex;
        }

        type = typeof(Exception);

        foreach (var catcher in this.Catchers) {
            if (catcher.ExceptionType != type) {
                continue;
            }

            catcher.Function.DynamicInvoke(ex);
            return null;
        }

        return ex;
    }

    /// <summary>
    /// 
    /// </summary>
    private class PromiseKeeper {
        /// <summary>
        /// 
        /// </summary>
        public object ReturnedValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Type InputType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Type OutputType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Delegate Function { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    private class PromiseCatcher {
        /// <summary>
        /// 
        /// </summary>
        public Type ExceptionType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Delegate Function { get; set; }
    }
}