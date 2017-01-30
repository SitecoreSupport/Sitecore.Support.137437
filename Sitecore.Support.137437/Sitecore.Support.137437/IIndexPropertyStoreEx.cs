using System;

namespace Sitecore.Support.ContentSearch.Maintenance
{
    public interface IIndexPropertyStoreEx
    {
        /// <summary>
        /// Incrementally adds value to the list by key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="postAction"></param>
        void Add(string key, string value, Action<string, string> postAction);

        /// <summary>
        /// Overwrites the value by key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="postAction"></param>
        void Set(string key, string value, Action<string, string> postAction);

        /// <summary>
        /// Returns the value by key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="postAction"></param>
        /// <returns></returns>
        string Get(string key, Action<string, string> postAction);
    }
}