namespace GitAutomation.Web
{
    public class ReserveScriptParameters
    {

        public ReserveScriptParameters(string name, ReserveFullState reserveFullState, string workingPath)
        {
            this.Name = name;
            this.ReserveFullState = reserveFullState;
            this.WorkingPath = workingPath;
        }

        public string Name { get; }
        public ReserveFullState ReserveFullState { get; }
        public string WorkingPath { get; }
    }
}