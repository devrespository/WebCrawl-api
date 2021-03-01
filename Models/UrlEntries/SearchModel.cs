using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.UrlEntries
{
    public class SearchModel
    {
        [Required]
        public string Url { get; set; }

        [Required]
        public string Expression { get; set; }
    }
}
