using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapMemoriesDownloader
{
    internal class Media
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonIgnore]
        public DateTime Date { get; set; }

        [JsonIgnore]
        public Uri ResolvedAWSUrl { get; set; }

        [JsonIgnore]
        public string AwsHost { get; set; }

        [JsonProperty("Date")]
        public string DateString { get; set; }

        [JsonProperty("Media Type")]
        public string Type { get; set; }

        [JsonProperty("Download Link")]
        public string Url { get; set; }
    }
}
