using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Plugins
{
    public static class PluginActivator
    {
        public static T GetPlugin<T>(string typeName, string errorMessage)
            where T : class
        {
            var pluginType = Type.GetType(typeName);
            if (pluginType == null)
            {
                throw new NotSupportedException(errorMessage);
            }
            return Activator.CreateInstance(pluginType) as T;
        }
    }
}
