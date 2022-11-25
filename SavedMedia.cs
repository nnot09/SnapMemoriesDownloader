using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapMemoriesDownloader
{
    internal class SavedMedia
    {
        [JsonProperty("Saved Media")]
        public List<Media> MediaItems { get; set; }
    }
}
