using GitAutomation.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace GitAutomation
{
    [TestClass]
    public class PowerShellExtensionsShould
    {
        [TestMethod]
        public void BeAbleToPassParametersToAnInlineScript()
        {
            using (var psInstance = PowerShell.Create())
            {
                psInstance
                    .AddScript(@"
param([string[]] $responseCollection)
return $responseCollection
")
                    .BindParametersToPowerShell(new
                    {
                        responseCollection = new[] { "apples", "oranges", "bananas" }
                    });

                var results = psInstance.Invoke<string>();
                Assert.IsTrue(results.SequenceEqual(new[] { "apples", "oranges", "bananas" }));
            }
        }

        [TestMethod]
        public void BeAbleToPassParametersToACommand()
        {
            using (var psInstance = PowerShell.Create())
            {
                psInstance
                    .AddUnrestrictedCommand("./Demo.ps1")
                    .BindParametersToPowerShell(new
                    {
                        responseCollection = new[] { "apples", "oranges", "bananas" }
                    });

                var results = psInstance.Invoke<string>();
                Assert.IsTrue(results.SequenceEqual(new[] { "apples", "oranges", "bananas" }));
            }
        }

        [TestMethod]
        public async Task BeAbleToStreamResponses()
        {
            using (var psInstance = PowerShell.Create())
            {
                psInstance
                    .AddScript(@"
#!/usr/bin/env pwsh

param([string[]] $responseCollection)
for ($i = 0; $i -lt $responseCollection.length; $i++)
{
	$responseCollection[$i]
	Start-Sleep -Seconds 1
}
")
                    .BindParametersToPowerShell(new
                    {
                        responseCollection = new[] { "apples", "oranges", "bananas" }
                    });


                var actualResults = new List<string>();
                var streamResponses = psInstance.InvokeAllStreams<string>(disposePowerShell: false);
                await foreach (var entry in streamResponses.SuccessAsync)
                {
                    Console.WriteLine($"Got '{entry}' at {DateTime.Now}");
                    actualResults.Add(entry);
                }
                Assert.IsTrue(actualResults.SequenceEqual(new[] { "apples", "oranges", "bananas" }));
            }
        }

        struct Foo { public string Name; }

        [TestMethod]
        public async Task BeAbleToParseArbitraryObjects()
        {

            using (var psInstance = PowerShell.Create())
            {
                psInstance
                    .AddScript(@"
#!/usr/bin/env pwsh

@{
    ""Name"" = 'baz'
}
");

                var actualResults = new List<Foo>();
                await foreach (var entry in psInstance.InvokeAllStreams<Foo>(disposePowerShell: false).SuccessAsync)
                {
                    Console.WriteLine($"Got '{entry}' at {DateTime.Now}");
                    actualResults.Add(entry);
                }
                Assert.IsTrue(actualResults.Count == 1);
                Assert.AreEqual("baz", actualResults[0].Name);
            }
        }

        [TestMethod]
        public async Task BeAbleToAccessResultsAfterPowerShellIsDisposed()
        {
            PowerShellStreams<Foo> streams;
            using (var psInstance = PowerShell.Create())
            {
                psInstance
                    .AddScript(@"
#!/usr/bin/env pwsh

@{
    ""Name"" = 'baz'
}
");

                streams = psInstance.InvokeAllStreams<Foo>(disposePowerShell: false);
                await streams.Completion;
            }

            var actualResults = new List<Foo>();
            await foreach (var entry in streams.SuccessAsync)
            {
                Console.WriteLine($"Got '{entry}' at {DateTime.Now}");
                actualResults.Add(entry);
            }
            Assert.IsTrue(actualResults.Count == 1);
            Assert.AreEqual("baz", actualResults[0].Name);
            Assert.IsTrue(streams.Success.Count == 1);
            Assert.AreEqual("baz", streams.Success[0].Name);
        }

        [TestMethod]
        public async Task BeAbleToAccessErrorsAfterPowerShellIsDisposed()
        {
            PowerShellStreams<Foo> streams;
            using (var psInstance = PowerShell.Create())
            {
                psInstance
                    .AddScript(@"
#!/usr/bin/env pwsh

Write-Error ""testing""
");

                streams = psInstance.InvokeAllStreams<Foo>(disposePowerShell: false);
                await streams.Completion;
            }

            Assert.IsTrue(streams.Error.Count == 1);
        }

        [TestMethod]
        public async Task GiveAccessToExceptionWithoutThrowing()
        {
            PowerShellStreams<Foo> streams;
            using (var psInstance = PowerShell.Create())
            {
                psInstance
                    .AddScript(@"
#!/usr/bin/env pwsh

""no close-quote to throw exception on purpose
");

                streams = psInstance.InvokeAllStreams<Foo>(disposePowerShell: false);
                await streams.Completion;
            }

            Assert.IsNotNull(streams.Exception);
        }
    }
}
