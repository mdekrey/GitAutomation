using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Processes
{

    public class ReactiveProcess : IReactiveProcess
    {
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
            foreach (var record in info.Environment)
            {
                resultInfo.Environment[record.Key] = record.Value;
            }
            var process = new Process() { StartInfo = resultInfo };
            process.EnableRaisingEvents = true;

            var processObservable = 
            ProcessExited = Observable.Create<Unit>(observer =>
            {
                process.Exited += delegate
                {
                    observer.OnCompleted();
                };
                process.Start();
                try
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                catch { }

                return () =>
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                };
            }).Publish().ConnectFirst();

            Output = 
                Observable.Merge(
                    from e in Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(
                        handler => process.OutputDataReceived += handler,
                        handler => process.OutputDataReceived -= handler
                    )
                    where e.EventArgs.Data != null
                    select new OutputMessage { Message = e.EventArgs.Data, Channel = OutputChannel.Out },
                    from e in Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(
                        handler => process.ErrorDataReceived += handler,
                        handler => process.ErrorDataReceived -= handler
                    )
                    where e.EventArgs.Data != null
                    select new OutputMessage { Message = e.EventArgs.Data, Channel = OutputChannel.Error }
                )
                .TakeUntil(this.ProcessExited.Concat(Observable.Return(Unit.Default)))
                .Concat(
                    Observable.Create<OutputMessage>(observer => {
                        observer.OnNext(
                            new OutputMessage { Channel = OutputChannel.ExitCode, ExitCode = process.ExitCode }
                        );
                        observer.OnCompleted();
                        return () => { };
                    })
                )
                .Publish().RefCount();
        }

        public IObservable<Unit> ProcessExited { get; }

        public IObservable<OutputMessage> Output { get; }
    }
}
