using System;

namespace NAnt.Core.Extensibility {
    public interface IPluginConsumer {
        void ConsumePlugin(IPlugin type);
    }
}