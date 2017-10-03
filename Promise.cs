using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharpPromise {
    /// <summary>
    /// 
    /// </summary>
    public class Promise {
        /// <summary>
        /// 
        /// </summary>
        public Promise() {
            this.FunctionEvents = new List<FunctionEvent>();
            this.CatchEvents = new List<CatchEvent>();
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
        private List<FunctionEvent> FunctionEvents { get; }

        /// <summary>
        /// 
        /// </summary>
        private List<CatchEvent> CatchEvents { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        public Promise Then(Delegate function) {
            this.FunctionEvents.Add(
                new FunctionEvent {
                    Function = function
                });

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        public Promise Then(Action function) {
            this.FunctionEvents.Add(
                new FunctionEvent {
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
        public Promise Then<T>(Func<T> function) {
            this.FunctionEvents.Add(
                new FunctionEvent {
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
            this.FunctionEvents.Add(
                new FunctionEvent {
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
            this.CatchEvents.Add(
                new CatchEvent {
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
            this.CatchEvents.Add(
                new CatchEvent {
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
            if (!this.FunctionEvents.Any()) {
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
            if (!this.FunctionEvents.Any()) {
                return this;
            }

            this.IsRunning = true;

            Exception UnhandledException = null;

            for (var i = 0; i < this.FunctionEvents.Count; i++) {
                try {
                    object output;

                    if (this.FunctionEvents[i].InputType == null) {
                        output = this.FunctionEvents[i].Function.DynamicInvoke();
                    }
                    else {
                        if (i == 0) {
                            if (initialValue == null) {
                                throw new NullReferenceException();
                            }

                            output = this.FunctionEvents[i].Function.DynamicInvoke(initialValue);
                        }
                        else {
                            var input = this.FunctionEvents[i - 1].ReturnedValue;

                            if (input == null) {
                                throw new NullReferenceException();
                            }

                            var inputCasted = Convert.ChangeType(input, this.FunctionEvents[i].InputType);

                            output = this.FunctionEvents[i].Function.DynamicInvoke(inputCasted);
                        }
                    }

                    if (this.FunctionEvents[i].OutputType == typeof(Promise)) {
                        var promise = (Promise) output;

                        promise.Wait();

                        output = promise.Result;
                    }

                    if (output != null) {
                        this.FunctionEvents[i].ReturnedValue = output;
                    }
                }
                catch (Exception ex) {
                    UnhandledException = this.HandleException(ex);
                    break;
                }
            }

            this.Result = this.FunctionEvents.Last().ReturnedValue;
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
        public Promise Wait() {
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

            foreach (var catcher in this.CatchEvents) {
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

            foreach (var catcher in this.CatchEvents) {
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
        private class FunctionEvent {
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
        private class CatchEvent {
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
}
