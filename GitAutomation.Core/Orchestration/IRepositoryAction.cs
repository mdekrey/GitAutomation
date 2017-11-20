using GitAutomation.Processes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.Orchestration
{
    public interface IRepositoryAction
    {
        /// <summary>
        /// Type of the action for persistence and translation
        /// </summary>
        string ActionType { get; }

        /// <summary>
        /// Parameters that can be used for persistence and translation to indicate the exact action being performed
        /// </summary>
        JToken Parameters { get; }

        /// <summary>
        /// Gets the output for later execution
        /// </summary>
        IObservable<IRepositoryActionEntry> ProcessStream { get; }

        /// <summary>
        /// Executes the action
        /// </summary>
        /// <param name="serviceProvider">The services to use to perform the action</param>
        /// <returns>Output of the action</returns>
        IObservable<IRepositoryActionEntry> PerformAction(IServiceProvider serviceProvider);
    }
}
