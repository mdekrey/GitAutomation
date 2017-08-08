using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            ProcessExited = Observable.Create<Unit>(observer =>
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.Exited += delegate
                {
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                };

                return () =>
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                };
            }).Publish().RefCount();

            Output = Observable.Merge(
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
                .TakeUntil(this.ProcessExited)
                .Merge(this.ProcessExited.Select(_ => new OutputMessage { Channel = OutputChannel.ExitCode, ExitCode = process.ExitCode }))
                .Publish().RefCount();
        }

        public IObservable<Unit> ProcessExited { get; }

        public IObservable<OutputMessage> Output { get; }
    }
}
