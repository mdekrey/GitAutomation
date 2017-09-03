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

            var processExited = Observable.Create<Unit>(observer =>
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
                // ProcessExited must be subscribed to or it'll never run.
                .Merge(processExited.Select(_ => default(OutputMessage)))
                .Concat(
                    Observable.Create<OutputMessage>(observer =>
                    {
                        observer.OnNext(
                            new OutputMessage { Channel = OutputChannel.ExitCode, ExitCode = process.ExitCode }
                        );
                        observer.OnCompleted();
                        return () => { };
                    })
                )
                .StartWith(new OutputMessage { Channel = OutputChannel.StartInfo, Message = process.StartInfo.FileName + " " + process.StartInfo.Arguments })
                .Publish().RefCount();
        }

        public IObservable<OutputMessage> Output { get; }
    }
}
