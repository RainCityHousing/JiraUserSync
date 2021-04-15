using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace JiraUserSync
{
    public class Value
    {
        [JsonProperty("accountId")]
        public string AccountId { get; set; }

        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("timeZone")]
        public string TimeZone { get; set; }
    }

    public class CustomerList
    {
        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("isLastPage")]
        public bool IsLastPage { get; set; }

        [JsonProperty("values")]
        public List<Value> Values { get; set; }
    }
    public class post
    {
        public String[] accountIds { get; set; }
    }
}
