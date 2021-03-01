using System.Collections.Generic;
using WebApi.Entities;
using WebApi.Models.UrlEntries;

namespace WebApi.Interfaces
{
    public interface IUrlEntriesService
    {
        IEnumerable<UrlEntries> GetAllEntries();
        void Search(SearchModel urlSearch);
        void AddUrls(List<UrlEntries> urlEntriesList);
    }
}
