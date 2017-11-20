using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Processes
{
    [TestClass]
    public class ReactiveProcessShould
    {
        static readonly IReactiveProcessFactory factory = new ReactiveProcessFactory();

        // TODO - these tests were built specifically with a Windows runner in
        // VisualStudio in mind. They will not pass in a Linux environment,
        // whether dockerized or not.

        [TestMethod]
        public async Task BeAbleToExecuteACommandAndGetResults()
        {
            var target = factory.BuildProcess(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c \"echo 1\""));

            var result = await target.ActiveOutput.ToArray();
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(OutputChannel.Out, result[0].Channel);
            Assert.AreEqual("1", result[0].Message);
            Assert.AreEqual(0, target.ExitCode);
        }

        [TestMethod]
        public async Task OnlyExecuteACommandOnce()
        {
            var target = factory.BuildProcess(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c \"echo 1\""));

            var firstResult = await target.ActiveOutput.ToArray();
            var result = await target.ActiveOutput.ToArray();
            Assert.AreEqual(0, result.Length);
        }
    }
}
