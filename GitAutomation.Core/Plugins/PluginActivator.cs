using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Plugins
{
    public static class PluginActivator
    {
        public static Type GetPluginTypeOrNull(string typeName)
        {
            var pluginType = typeName == null ? null : Type.GetType(typeName);
            return pluginType;
        }

        public static T GetPluginOrNull<T>(string typeName)
            where T : class
        {
            var pluginType = GetPluginTypeOrNull(typeName);
            if (pluginType == null)
            {
                return null;
            }
            return Activator.CreateInstance(pluginType) as T;
        }

        public static T GetPlugin<T>(string typeName, string errorMessage)
            where T : class
        {
            var pluginType = GetPluginTypeOrNull(typeName);
            if (pluginType == null)
            {
                throw new NotSupportedException(errorMessage);
            }
            return Activator.CreateInstance(pluginType) as T;
        }
    }
}
