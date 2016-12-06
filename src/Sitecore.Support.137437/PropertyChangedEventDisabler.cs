using Sitecore.Common;
using Sitecore.Data.Events;
using System;

namespace Sitecore.Support.ContentSearch.Maintenance
{
    internal class PropertyChangedEventDisabler : IDisposable
    {
        public PropertyChangedEventDisabler(bool state)
        {
            EventDisablerState objectToSwitchTo = state ? EventDisablerState.Enabled : EventDisablerState.Disabled;
            Switcher<EventDisablerState, EventDisabler>.Enter(objectToSwitchTo);
        }

        public void Dispose()
        {
            Switcher<EventDisablerState, EventDisabler>.Exit();
        }
    }
}
