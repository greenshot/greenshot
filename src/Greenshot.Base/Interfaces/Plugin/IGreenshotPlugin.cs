using System;

namespace Greenshot.Base.Interfaces.Plugin
{
    /// <summary>
    /// This defines the plugin
    /// </summary>
    public interface IGreenshotPlugin : IDisposable
    {
        /// <summary>
        /// Is called after the plugin is instantiated, the Plugin should keep a copy of the host and pluginAttribute.
        /// </summary>
        /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
        bool Initialize();

        /// <summary>
        /// Unload of the plugin
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Open the Configuration Form, will/should not be called before handshaking is done
        /// </summary>
        void Configure();

        /// <summary>
        /// Define the name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        bool IsConfigurable { get; }
    }
}