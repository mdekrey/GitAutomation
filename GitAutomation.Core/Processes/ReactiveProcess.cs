using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Processes
{

    public class ReactiveProcess : IReactiveProcess
    {
        int exitCode;

        public string StartInfo { get; }
        public ProcessState State { get; set; }

        public int ExitCode
        {
            get
            {
                if (State == ProcessState.Exited)
                {
                    return exitCode;
                }
                throw new InvalidOperationException();
            }
        }

        public IObservable<OutputMessage> ActiveOutput { get; }

        public IObservable<ProcessState> ActiveState { get; }
        public ImmutableList<OutputMessage> Output { get; private set; } = ImmutableList<OutputMessage>.Empty;

        public ReactiveProcess(ProcessStartInfo info)
        {
            var resultInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                StandardOutputEncoding = info.StandardOutputEncoding,
                StandardErrorEncoding = info.StandardErrorEncoding,
                RedirectStandardOutput = true,
                RedirectStandardInput = info.RedirectStandardInput,
                WorkingDirectory = info.WorkingDirectory,
                FileName = info.FileName,
                Arguments = info.Arguments,
                RedirectStandardError = true,
            };
            StartInfo = resultInfo.FileName + " " + resultInfo.Arguments;
            foreach (var record in info.Environment)
            {
                resultInfo.Environment[record.Key] = record.Value;
            }
            var process = new Process() { StartInfo = resultInfo };
            process.EnableRaisingEvents = true;

            ActiveState = Observable.Create<ProcessState>(observer =>
            {
                observer.OnNext(ProcessState.NotStarted);
                process.Exited += delegate
                {
                    exitCode = process.ExitCode;
                    observer.OnNext(ProcessState.Exited);
                    observer.OnCompleted();
                };
                process.Start();
                observer.OnNext(ProcessState.Running);
                try
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                catch { }
                var timer = Observable.Interval(TimeSpan.FromSeconds(1000))
                    .Subscribe(_ =>
                    {
                        if (process.HasExited)
                        {
                            observer.OnNext(ProcessState.Exited);
                            observer.OnCompleted();
                        }
                    });

                return () =>
                {

                    timer.Dispose();
                    if (!process.HasExited)
                    {
                        State = ProcessState.Cancelled;
                        process.Kill();
                    }
                    else
                    {
                        State = ProcessState.Exited;
                        exitCode = process.ExitCode;
                    }
                };
            })
                .Do(state => State = state)
                .Replay(1)
                .ConnectFirst();

            var output =
                Observable.Merge(
                    (
                        from e in Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(
                            handler => process.OutputDataReceived += handler,
                            handler => process.OutputDataReceived -= handler
                        )
                        select new OutputMessage { Message = e.EventArgs.Data, Channel = OutputChannel.Out }
                    ).TakeWhile(msg => msg.Message != null),
                    (
                        from e in Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(
                            handler => process.ErrorDataReceived += handler,
                            handler => process.ErrorDataReceived -= handler
                        )
                        select new OutputMessage { Message = e.EventArgs.Data, Channel = OutputChannel.Error }
                    ).TakeWhile(msg => msg.Message != null)
                )
                .Do(message => Output = Output.Add(message))
                .Publish();
            output.Connect();

            ActiveOutput = Observable.Create<OutputMessage>(observer =>
            {
                return new CompositeDisposable(
                    output.Subscribe(observer),
                    ActiveState.Subscribe()
                );
            });
        }

    }
}
