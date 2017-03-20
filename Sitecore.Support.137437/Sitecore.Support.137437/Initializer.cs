namespace Sitecore.Support.ContentSearch.Hooks
{
    public class Initializer : Sitecore.ContentSearch.Hooks.Initializer
    {
        public override void Initialize()
        {
            using (new Maintenance.PropertyChangedEventDisabler(true))
            {
                base.Initialize();
            }
        }
    }
}