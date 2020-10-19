// <copyright file="RxCommand.cs">
//     Copyright (c) 2018 Kirk Woll All rights reserved.
// </copyright>
/*
 * From SexyReact https://github.com/kswoll/sexy-react
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Windows.Input;

namespace Curfew
{
    public partial interface IRxCommand : ICommand, ICanInvoke, IObservable<Unit>
    {
        Task InvokeAsync();
    }

    public partial interface IRxCommand<in TInput> : ICanInvoke, IObservable<Unit>
    {
        Task InvokeAsync(TInput input);
    }

    public interface ICanInvoke
    {
        IObservable<bool> CanInvoke { get; }
    }

    public class RxCommand<TInput> :
        RxCommand<TInput, Unit>,
        IRxCommand,
        IRxCommand<TInput>
    {

        /// <summary>
        /// You are free to create commands using this constructor, but you may find it more convenient to use one of
        /// the factory methods in RxCommand and RxFunction.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="defaultValue">A factory function to provide the return value for when the method fails to execute.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public RxCommand(Func<TInput, Task<Unit>> action, IObservable<bool> canExecute = null, Func<Unit> defaultValue = null, bool allowSimultaneousExecution = false)
            : base(action, canExecute, defaultValue, allowSimultaneousExecution)
        {
        }

        Task IRxCommand.InvokeAsync()
        {
            return InvokeAsync(default(TInput));
        }

        Task IRxCommand<TInput>.InvokeAsync(TInput input)
        {
            return InvokeAsync(input);
        }
    }

    public partial class RxCommand<TInput, TOutput> :
        IRxFunction<TOutput>,
        IRxFunction<TInput, TOutput>
    {
        private Lazy<IObservable<bool>> canExecute;
        private Func<TInput, Task<TOutput>> action;
        private Lazy<Subject<TOutput>> subject = new Lazy<Subject<TOutput>>(() => new Subject<TOutput>());
        private Lazy<ReplaySubject<bool>> isExecuting = new Lazy<ReplaySubject<bool>>(() =>
        {
            var subject = new ReplaySubject<bool>(1);
            subject.OnNext(false);      // Initialize with a default value
                return subject;
        });
        private object lockObject = new object();
        private bool isSubscribedToCanExecute;
        private bool isAllowedToExecute = true;
        private Func<TOutput> defaultValue;

        /// <summary>
        /// You are free to create commands using this constructor, but you may find it more convenient to use one of
        /// the factory methods in RxCommand and RxFunction.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="defaultValue">A factory function to provide the return value for when the method fails to execute.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public RxCommand(Func<TInput, Task<TOutput>> action, IObservable<bool> canExecute = null, Func<TOutput> defaultValue = null, bool allowSimultaneousExecution = false)
        {
            this.action = action;
            this.defaultValue = defaultValue ?? (() => default(TOutput));

            Func<IObservable<bool>> canExecuteFactory;
            if (allowSimultaneousExecution)
            {
                if (canExecute == null)
                    canExecuteFactory = () => Observable.Return(true);
                else
                    canExecuteFactory = () => canExecute;
            }
            else
            {
                if (canExecute == null)
                    canExecuteFactory = () => IsExecuting.Select(x => !x);
                else
                    canExecuteFactory = () => IsExecuting.SelectMany(x => canExecute.Select(y => !x && y));
            }

            this.canExecute = new Lazy<IObservable<bool>>(canExecuteFactory);

            OnCreated();
        }

        partial void OnCreated();

        public IObservable<bool> CanInvoke => canExecute.Value;
        public IObservable<bool> IsExecuting => isExecuting.Value;

        public IDisposable Subscribe(IObserver<TOutput> observer)
        {
            return subject.Value.Subscribe(observer);
        }

        public void Invoke(TInput input)
        {
            InvokeAsync(input).RunAsync();
        }

        /// <summary>
        /// Executes the task asynchronously.  The observable represented by this command emits its next value *before*
        /// this method completes and returns its value.
        /// </summary>
        public async Task<TOutput> InvokeAsync(TInput input)
        {
            lock (lockObject)
            {
                if (!isSubscribedToCanExecute)
                {
                    CanInvoke.Subscribe(UpdateIsAllowedToExecute);
                    isSubscribedToCanExecute = true;
                }
                if (!isAllowedToExecute)
                {
                    return defaultValue();
                }
            }

            isExecuting.Value.OnNext(true);

            try
            {
                var result = await action(input);
                if (subject.IsValueCreated)
                    subject.Value.OnNext(result);

                isExecuting.Value.OnNext(false);

                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception executing RxCommand");
                Debug.WriteLine(e.ToString());
                if (subject.IsValueCreated)
                    subject.Value.OnError(e);
                isExecuting.Value.OnNext(false);
                return default(TOutput);
            }
        }

        private void UpdateIsAllowedToExecute(bool value)
        {
            lock (lockObject)
            {
                isAllowedToExecute = value;
            }
        }

        Task<TOutput> IRxFunction<TOutput>.InvokeAsync()
        {
            return InvokeAsync(default(TInput));
        }

        Task<TOutput> IRxFunction<TInput, TOutput>.InvokeAsync(TInput input)
        {
            return InvokeAsync(input);
        }
    }

    public partial class RxCommand<TInput, TOutput> : ICommand
    {
        private EventHandler canExecuteChanged;
        private bool canExecuteValue;

        partial void OnCreated()
        {
            CanInvoke.Subscribe(OnCanExecuteChanged);
        }

        protected void OnCanExecuteChanged(bool canExecute)
        {
            canExecuteValue = canExecute;
            canExecuteChanged?.Invoke(this, new EventArgs());
        }

        bool ICommand.CanExecute(object parameter)
        {
            return canExecuteValue;
        }

        void ICommand.Execute(object parameter)
        {
            Invoke(parameter == null ? default(TInput) : (TInput)parameter);
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { canExecuteChanged = (EventHandler)Delegate.Combine(canExecuteChanged, value); }
            remove { canExecuteChanged = (EventHandler)Delegate.Remove(canExecuteChanged, value); }
        }
    }



    public partial interface IRxFunction<TOutput> : IObservable<TOutput>, ICanInvoke
    {
        Task<TOutput> InvokeAsync();
    }

    public partial interface IRxFunction<in TInput, TOutput> : IObservable<TOutput>, ICanInvoke
    {
        Task<TOutput> InvokeAsync(TInput input);
    }

    public static class TaskExtensions
    {
        public static void RunAsync(this Task task)
        {
        }
    }

    public static partial class RxCommandExtensions
    {
        /// <summary>
        /// Executes the command synchronously by calling Wait() on the async task.  This should generally only be
        /// called when you know the action will execute synchronously.  Otherwise you will likely face potential
        /// deadlocks.
        /// </summary>
        public static void Invoke(this IRxCommand command)
        {
            command.InvokeAsync().Wait();
        }

        /// <summary>
        /// Executes the command synchronously by calling Wait() on the async task.  This should generally only be
        /// called when you know the action will execute synchronously.  Otherwise you will likely face potential
        /// deadlocks.
        /// </summary>
        public static void Invoke<TInput>(this IRxCommand<TInput> command, TInput input)
        {
            command.InvokeAsync(input).Wait();
        }

        /// <summary>
        /// Executes the command synchronously by returning Result on the async task.  This should generally only be
        /// called when you know the action will execute synchronously.  Otherwise you will likely face potential
        /// deadlocks.
        /// </summary>
        public static TOutput Invoke<TOutput>(this IRxFunction<TOutput> command)
        {
            return command.InvokeAsync().Result;
        }

        /// <summary>
        /// Executes the command synchronously by directly returning Result on the async task.  This should generally only be
        /// called when you know the action will execute synchronously.  Otherwise you will likely face potential
        /// deadlocks.
        /// </summary>
        public static TOutput Invoke<TInput, TOutput>(this IRxFunction<TInput, TOutput> command, TInput input)
        {
            return command.InvokeAsync(input).Result;
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxCommand Combine(this IRxCommand first, IRxCommand second)
        {
            return RxCommand.Create(async () =>
            {
                await first.InvokeAsync();
                await second.InvokeAsync();
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  The first command
        /// expects TInput as an argument, and the returned command also expects TInput when executed.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxCommand<TInput> Combine<TInput>(this IRxCommand<TInput> first, IRxCommand second)
        {
            return RxCommand.Create<TInput>(async x =>
            {
                await first.InvokeAsync(x);
                await second.InvokeAsync();
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  Both commands
        /// expect TInput as an argument, and the returned command also expects TInput when executed.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxCommand<TInput> Combine<TInput>(this IRxCommand<TInput> first, IRxCommand<TInput> second)
        {
            return RxCommand.Create<TInput>(async x =>
            {
                await first.InvokeAsync(x);
                await second.InvokeAsync(x);
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  The second command
        /// expects TInput as an argument, and the returned command also expects TInput when executed.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxCommand<TInput> Combine<TInput>(this IRxCommand first, IRxCommand<TInput> second)
        {
            return RxCommand.Create<TInput>(async x =>
            {
                await first.InvokeAsync();
                await second.InvokeAsync(x);
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  The second command
        /// returns TOutput, and the returned command also returns TOutput when executed.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TOutput> Combine<TOutput>(this IRxCommand first, IRxFunction<TOutput> second)
        {
            return RxFunction.CreateAsync(async () =>
            {
                await first.InvokeAsync();
                return await second.InvokeAsync();
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  The second command
        /// expects TInput as a parameter and returns TOutput.  The returned command expects TInput and returns
        /// TOutput when executed.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TInput, TOutput> Combine<TInput, TOutput>(this IRxCommand first, IRxFunction<TInput, TOutput> second)
        {
            return RxFunction.CreateAsync<TInput, TOutput>(async x =>
            {
                await first.InvokeAsync();
                return await second.InvokeAsync(x);
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  The first command
        /// expects TInput as a parameter.  The second command returns TOutput. The returned command expects
        /// TInput and returns TOutput when executed.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TInput, TOutput> Combine<TInput, TOutput>(this IRxCommand<TInput> first, IRxFunction<TOutput> second)
        {
            return RxFunction.CreateAsync<TInput, TOutput>(async x =>
            {
                await first.InvokeAsync(x);
                return await second.InvokeAsync();
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  Both commands
        /// expect TInput as a parameter.  The second command returns TOutput. The returned command expects
        /// TInput and returns TOutput when executed.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TInput, TOutput> Combine<TInput, TOutput>(this IRxCommand<TInput> first, IRxFunction<TInput, TOutput> second)
        {
            return RxFunction.CreateAsync<TInput, TOutput>(async x =>
            {
                await first.InvokeAsync(x);
                return await second.InvokeAsync(x);
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  The first command
        /// returns TOutput.  The returned command also returns TOutput.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TOutput> Combine<TOutput>(this IRxFunction<TOutput> first, IRxCommand second)
        {
            return RxFunction.CreateAsync(async () =>
            {
                var result = await first.InvokeAsync();
                await second.InvokeAsync();
                return result;
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  The first command
        /// expects TInput as an argument and returns TOutput. The returned command also expects TInput when executed
        /// and returns TOutput.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TInput, TOutput> Combine<TInput, TOutput>(this IRxFunction<TInput, TOutput> first, IRxCommand second)
        {
            return RxFunction.CreateAsync<TInput, TOutput>(async x =>
            {
                var result = await first.InvokeAsync(x);
                await second.InvokeAsync();
                return result;
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  Both commands
        /// expect TInput as an argument, and the second command also returns TOutput.  The returned command
        /// also expects TInput when executed and returns TOutput.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TInput, TOutput> Combine<TInput, TOutput>(this IRxFunction<TInput, TOutput> first, IRxCommand<TInput> second)
        {
            return RxFunction.CreateAsync<TInput, TOutput>(async x =>
            {
                var result = await first.InvokeAsync(x);
                await second.InvokeAsync(x);
                return result;
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  The first command returns
        /// TOutput. The second command expects TInput as an argument. The returned command also expects TInput when executed
        /// and returns TOutput.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TInput, TOutput> Combine<TInput, TOutput>(this IRxFunction<TOutput> first, IRxCommand<TInput> second)
        {
            return RxFunction.CreateAsync<TInput, TOutput>(async x =>
            {
                var result = await first.InvokeAsync();
                await second.InvokeAsync(x);
                return result;
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  The output of the first command
        /// is fed to the input of the second command.  The returned command returns TSecondOutput when executed.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TSecondOutput> Combine<TFirstOutput, TSecondOutput>(this IRxFunction<TFirstOutput> first, IRxFunction<TFirstOutput, TSecondOutput> second)
        {
            return RxFunction.CreateAsync(async () =>
            {
                var firstResult = await first.InvokeAsync();
                return await second.InvokeAsync(firstResult);
            });
        }

        /// <summary>
        /// Executes the first command and then when it completes executes the second command.  TInput is passed to the
        /// first command and the output of that command is fed in as the argument to the seond command. The returned command
        /// expects TInput and returns TSecondOutput when executed.
        /// </summary>
        /// <param name="first">The command to execute first</param>
        /// <param name="second">The command to execute second</param>
        /// <returns>The new command that executes both commands in turn.</returns>
        public static IRxFunction<TInput, TSecondOutput> Combine<TInput, TFirstOutput, TSecondOutput>(this IRxFunction<TInput, TFirstOutput> first, IRxFunction<TFirstOutput, TSecondOutput> second)
        {
            return RxFunction.CreateAsync<TInput, TSecondOutput>(async x =>
            {
                var firstResult = await first.InvokeAsync(x);
                return await second.InvokeAsync(firstResult);
            });
        }

        /// <summary>
        /// Converts an unparameterized command into a command parameterized by Unit.  This can be useful with methods such
        /// as Combine which expect particular types of commands that may not fit the command you have in order to
        /// get the behavor you otherwise expect.
        /// </summary>
        public static IRxCommand<Unit> AsParameterized(this IRxCommand command)
        {
            return RxCommand.Create<Unit>(_ => command.InvokeAsync());
        }

        /// <summary>
        /// Converts an unparameterized command into a command parameterized by Unit.  This can be useful with methods such
        /// as Combine which expect particular types of commands that may not fit the command you have in order to
        /// get the behavor you otherwise expect.
        /// </summary>
        public static IRxFunction<Unit, TOutput> AsParameterized<TOutput>(this IRxFunction<TOutput> command)
        {
            return RxFunction.CreateAsync<Unit, TOutput>(_ => command.InvokeAsync());
        }

        /// <summary>
        /// Converts an IRxFunction&lt;TOutput&gt; into an IRxCommand.  This can be useful with methods such as
        /// Combine which expect particular types of commands that may not fit the command you have in order to
        /// get the behavor you otherwise expect.  The value returned from the function is simply discarded.
        /// </summary>
        public static IRxCommand AsCommand<TOutput>(this IRxFunction<TOutput> function)
        {
            return RxCommand.Create(() => function.InvokeAsync());
        }

        /// <summary>
        /// Converts an IRxFunction&lt;TOutput&gt; into an IRxCommand.  This can be useful with methods such as
        /// Combine which expect particular types of commands that may not fit the command you have in order to
        /// get the behavor you otherwise expect.  The value returned from the function is simply discarded.
        /// </summary>
        public static IRxCommand<TInput> AsCommand<TInput, TOutput>(this IRxFunction<TInput, TOutput> function)
        {
            return RxCommand.Create<TInput>(x => function.InvokeAsync(x));
        }
    }

    public static class RxCommand
    {
        /// <summary>
        /// Creates a command that consumes no input and produces no output.  Non async version.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxCommand Create(Action action, IObservable<bool> canExecute = null, bool allowSimultaneousExecution = false)
        {
            return CreateAsync(
                () =>
                {
                    action();
                    return Task.FromResult(default(Unit));
                },
                canExecute,
                allowSimultaneousExecution: allowSimultaneousExecution);
        }

        /// <summary>
        /// Creates a command that consumes no input and produces no output.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxCommand CreateAsync(Func<Task> action, IObservable<bool> canExecute = null, bool allowSimultaneousExecution = false)
        {
            return new RxCommand<Unit>(
                async x =>
                {
                    await action();
                    return default(Unit);
                },
                canExecute,
                allowSimultaneousExecution: allowSimultaneousExecution);
        }

        /// <summary>
        /// Creates a command that consumes input, but produces no output.  Non async version.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxCommand<TInput> Create<TInput>(Action<TInput> action, IObservable<bool> canExecute = null, bool allowSimultaneousExecution = false)
        {
            return new RxCommand<TInput>(
                x =>
                {
                    action(x);
                    return Task.FromResult(default(Unit));
                },
                canExecute,
                allowSimultaneousExecution: allowSimultaneousExecution);
        }

        /// <summary>
        /// Creates a command that consumes input, but produces no output.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxCommand<TInput> CreateAsync<TInput>(Func<TInput, Task> action, IObservable<bool> canExecute = null, bool allowSimultaneousExecution = false)
        {
            return new RxCommand<TInput>(
                async x =>
                {
                    await action(x);
                    return default(Unit);
                },
                canExecute,
                allowSimultaneousExecution: allowSimultaneousExecution);
        }
    }

    public static class RxFunction
    {
        /// <summary>
        /// Creates a command that consumes no input, but produces output.  Non async version.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="defaultValue">A factory function to provide the return value for when the method fails to execute.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxFunction<TOutput> Create<TOutput>(Func<TOutput> action, IObservable<bool> canExecute = null, Func<TOutput> defaultValue = null, bool allowSimultaneousExecution = false)
        {
            return new RxCommand<Unit, TOutput>(x => Task.FromResult(action()), canExecute, defaultValue, allowSimultaneousExecution);
        }

        /// <summary>
        /// Creates a command that consumes no input, but produces output.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="defaultValue">A factory function to provide the return value for when the method fails to execute.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxFunction<TOutput> CreateAsync<TOutput>(Func<Task<TOutput>> action, IObservable<bool> canExecute = null, Func<TOutput> defaultValue = null, bool allowSimultaneousExecution = false)
        {
            return new RxCommand<Unit, TOutput>(x => action(), canExecute, defaultValue, allowSimultaneousExecution);
        }

        /// <summary>
        /// Creates a command that consumes input and produces output.  Non async version.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="defaultValue">A factory function to provide the return value for when the method fails to execute.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxFunction<TInput, TOutput> Create<TInput, TOutput>(Func<TInput, TOutput> action, IObservable<bool> canExecute = null, Func<TOutput> defaultValue = null, bool allowSimultaneousExecution = false)
        {
            return RxFunction<TInput>.Create(action, canExecute, defaultValue, allowSimultaneousExecution);
        }

        /// <summary>
        /// Creates a command that consumes input and produces output.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="defaultValue">A factory function to provide the return value for when the method fails to execute.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxFunction<TInput, TOutput> CreateAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> action, IObservable<bool> canExecute = null, Func<TOutput> defaultValue = null, bool allowSimultaneousExecution = false)
        {
            return RxFunction<TInput>.CreateAsync(action, canExecute, defaultValue, allowSimultaneousExecution);
        }
    }

    /// <summary>
    /// This class facilitates creation functions with lambdas so the compiler can still infer the output type even though
    /// it can't infer the input type.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    public static class RxFunction<TInput>
    {
        /// <summary>
        /// Creates a command that consumes input and produces output.  Non async version.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="defaultValue">A factory function to provide the return value for when the method fails to execute.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxFunction<TInput, TOutput> Create<TOutput>(Func<TInput, TOutput> action, IObservable<bool> canExecute = null, Func<TOutput> defaultValue = null, bool allowSimultaneousExecution = false)
        {
            return new RxCommand<TInput, TOutput>(x => Task.FromResult(action(x)), canExecute, defaultValue, allowSimultaneousExecution);
        }

        /// <summary>
        /// Creates a command that consumes input and produces output.
        /// </summary>
        /// <param name="action">The action to execute when invoking the command.</param>
        /// <param name="canExecute">An observable that dictates whether or not the command may execute. If not
        /// specified, an observable is created that produces true.</param>
        /// <param name="defaultValue">A factory function to provide the return value for when the method fails to execute.</param>
        /// <param name="allowSimultaneousExecution">If true, multiple execution of this command may be performed.  If false,
        /// then subsequent calls to ExecuteAsync return the defaultValue until the execution of the initial invocation completes.</param>
        public static IRxFunction<TInput, TOutput> CreateAsync<TOutput>(Func<TInput, Task<TOutput>> action, IObservable<bool> canExecute = null, Func<TOutput> defaultValue = null, bool allowSimultaneousExecution = false)
        {
            return new RxCommand<TInput, TOutput>(action, canExecute, defaultValue, allowSimultaneousExecution);
        }
    }
}
