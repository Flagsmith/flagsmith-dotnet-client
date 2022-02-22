using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Organization.Models;
namespace FlagsmithEngine.Project.Models
{
    public class ProjectModel
    {
        [JsonProperty(PropertyName = "hide_disabled_flags")]
        public bool HideDisabledFlags { get; set; }
        [JsonProperty(PropertyName = "segments")]
        public List<SegmentModel> Segments { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }
        [JsonProperty(PropertyName = "organisation")]
        public OrganisationModel Organisation { get; set; }
    }
}
