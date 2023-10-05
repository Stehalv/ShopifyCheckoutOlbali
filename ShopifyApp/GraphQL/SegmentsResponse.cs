
using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ShopifyApp.GraphQL.SegmentModel
{
    public partial class Result
    {
        [JsonProperty("data")]
        public Data Data { get; set; }

        [JsonProperty("extensions")]
        public Extensions Extensions { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("segments")]
        public Segments Segments { get; set; }
    }

    public partial class Segments
    {
        [JsonProperty("edges")]
        public Edge[] Edges { get; set; }
    }

    public partial class Edge
    {
        [JsonProperty("node")]
        public Node Node { get; set; }
    }

    public partial class Node
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class Extensions
    {
        [JsonProperty("cost")]
        public Cost Cost { get; set; }
    }

    public partial class Cost
    {
        [JsonProperty("requestedQueryCost")]
        public long RequestedQueryCost { get; set; }

        [JsonProperty("actualQueryCost")]
        public long ActualQueryCost { get; set; }

        [JsonProperty("throttleStatus")]
        public ThrottleStatus ThrottleStatus { get; set; }
    }

    public partial class ThrottleStatus
    {
        [JsonProperty("maximumAvailable")]
        public long MaximumAvailable { get; set; }

        [JsonProperty("currentlyAvailable")]
        public long CurrentlyAvailable { get; set; }

        [JsonProperty("restoreRate")]
        public long RestoreRate { get; set; }
    }

    public partial class Result
    {
        public static Result FromJson(string json) => JsonConvert.DeserializeObject<Result>(json, SegmentModel.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Result self) => JsonConvert.SerializeObject(self, SegmentModel.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}