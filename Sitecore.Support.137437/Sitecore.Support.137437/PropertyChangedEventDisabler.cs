using Sitecore.Common;
using Sitecore.Data.Events;
using System;

namespace Sitecore.Support.ContentSearch.Maintenance
{
    internal class PropertyChangedEventDisabler : IDisposable
    {
        public PropertyChangedEventDisabler(bool state)
        {
            Switcher<EventDisablerState, EventDisabler>.Enter(state ? EventDisablerState.Enabled : EventDisablerState.Disabled);
        }

        public void Dispose()
        {
            Switcher<EventDisablerState, EventDisabler>.Exit();
        }
    }
}
