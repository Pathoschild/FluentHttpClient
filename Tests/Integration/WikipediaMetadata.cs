using System;
using Newtonsoft.Json;

namespace Pathoschild.Http.Tests.Integration
{
    /// <summary>A representative object mapped to a Wikipedia API endpoint.</summary>
    /// <remarks>The API returns a nested data structure (query.general.*). We
    /// could make the client deserialize into a single object, but for the
    /// sake of simplicity we'll just replicate the data structure.</remarks>
    public class WikipediaMetadata
    {
        /// <summary>The query response.</summary>
        public WikipediaQuery Query { get; set; }

        /// <summary>The query response object.</summary>
        public class WikipediaQuery
        {
            /// <summary>The general metadata about the English Wikipedia.</summary>
            public WikipediaGeneral General { get; set; }
        }

        /// <summary>The general metadata object about the English Wikipedia.</summary>
        public class WikipediaGeneral
        {
            /// <summary>The name of the default article.</summary>
            public string MainPage { get; set; }

            /// <summary>The URL of the default article.</summary>
            public string Base { get; set; }

            /// <summary>The friendly name of the project.</summary>
            public string SiteName { get; set; }

            /// <summary>The ISO-639 2 language code which covers content on this wiki.</summary>
            [JsonProperty("lang")]
            public string Language { get; set; }

            /// <summary>The URI format for articles relative to the domain (where $1 is the article title).</summary>
            public string ArticlePath { get; set; }

            /// <summary>The base URI for MediaWiki scripts relative to the domain.</summary>
            public string ScriptPath { get; set; }

            /// <summary>The protocol-relative domain portion of the site URL.</summary>
            public string Server { get; set; }

            /// <summary>The internal identifier for this wiki which is unique within the wiki farm.</summary>
            public string WikiID { get; set; }

            /// <summary>Whether article paths depend on language variants.</summary>
            public bool VariantArticlePath { get; set; }

            /// <summary>The current time when the API response was generated.</summary>
            public DateTime Time { get; set; }

            /// <summary>The maximum file upload size.</summary>
            public long MaxUploadSize { get; set; }
        }
    }
}