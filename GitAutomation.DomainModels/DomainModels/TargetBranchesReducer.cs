using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels
{
    public class TargetBranchesReducer
    {
        public static TargetBranchesState Reduce(TargetBranchesState original, StandardAction action) =>
            action.Action switch
        {
            _ => original
        };

    }
}
