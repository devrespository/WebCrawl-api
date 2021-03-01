using System;

namespace WebApi.Models.UrlEntries
{
    public class DetailsModel
    {
        public DateTimeOffset Date { get; set; }

        public Uri Url { get; set; }

        public string EnteredExpression { get; set; }

        public int NumberOfHits { get; set; }
    }
}
