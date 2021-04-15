using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace JiraUserSync
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 0 19 * * *")] TimerInfo myTimer, ILogger log)//should be  daily 7pm
        //public static void Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            //dicts to hold customer / org customer lists
            Dictionary<string, string> custListDict = new Dictionary<string, string>();
            Dictionary<string, string> orgListDict = new Dictionary<string, string>();

            //Initial API calls
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("X-ExperimentalAPI", "opt-in");
            var customers = JiraAPICall("https://raincityhousing.atlassian.net/rest/servicedeskapi/servicedesk/OP/customer", Method.GET, "default", "default", headers);
            var orgMembers = JiraAPICall("https://raincityhousing.atlassian.net/rest/servicedeskapi/organization/3/user", Method.GET, "default", "default", headers);
           
            CustomerList customerlist = JsonConvert.DeserializeObject<CustomerList>(customers.Content);
            foreach (var y in customerlist.Values)
            {
                //Grab all active customers
                if (y.EmailAddress != null && y.EmailAddress != "")
                    if (y.Active)
                        custListDict.Add(y.AccountId, y.EmailAddress);
            }
            //API call limited to 50 customers / call, repeat call and add to list for all that remain
            int currentIndex = customerlist.Start + customerlist.Size;
            while (customerlist.IsLastPage == false)
            {
                customers = JiraAPICall("https://raincityhousing.atlassian.net/rest/servicedeskapi/servicedesk/OP/customer?start=" + currentIndex, Method.GET, "default", "default", headers);
                customerlist = JsonConvert.DeserializeObject<CustomerList>(customers.Content);
                foreach (var y in customerlist.Values)
                {
                    if (y.EmailAddress != null && y.EmailAddress != "")
                        if (y.Active)
                            custListDict.Add(y.AccountId, y.EmailAddress);
                }
                currentIndex = customerlist.Start + customerlist.Size;
            }
            log.LogInformation("Customer list size is: " + custListDict.Count);

            //reset index, do same thing for org members
            currentIndex = 0;
            CustomerList orgList = JsonConvert.DeserializeObject<CustomerList>(orgMembers.Content);
            foreach (var y in orgList.Values)
                orgListDict.Add(y.AccountId, y.EmailAddress);
            currentIndex = orgList.Start + orgList.Size;
            while (orgList.IsLastPage == false)
            {
                orgMembers = JiraAPICall("https://raincityhousing.atlassian.net/rest/servicedeskapi/organization/3/user?start=" + currentIndex, Method.GET, "default", "default", headers);
                orgList = JsonConvert.DeserializeObject<CustomerList>(orgMembers.Content);
                foreach (var y in orgList.Values)
                {
                    if(!orgListDict.ContainsKey(y.AccountId))
                    orgListDict.Add(y.AccountId, y.EmailAddress);
                }
                currentIndex = orgList.Start + orgList.Size;
            }
            log.LogInformation("org list size is: " + orgListDict.Count);

            //all customers EXCEPT those already in org list are new customers to add to org
            var accountIds = custListDict.Keys.Except(orgListDict.Keys);
            post Post = new post();
            Post.accountIds = accountIds.ToArray<string>();
            log.LogInformation("Adding " + Post.accountIds.Count() + " people to org");
            if (Post.accountIds.Length > 0)
            {
                string body = JsonConvert.SerializeObject(Post);
                var re = JiraAPICall("https://raincityhousing.atlassian.net/rest/servicedeskapi/organization/3/user", Method.POST, body);
            }
            else
            {
                log.LogInformation("No new customers to add");
            }
            var accountIdsToRemove = orgListDict.Keys.Except(custListDict.Keys);
            Post.accountIds = accountIdsToRemove.ToArray<string>();
            log.LogInformation("Customers to remove from org: " + accountIdsToRemove.Count());
            if (Post.accountIds.Length > 0)
            {
                string body = JsonConvert.SerializeObject(Post);
                var re = JiraAPICall("https://raincityhousing.atlassian.net/rest/servicedeskapi/organization/3/user", Method.DELETE, body);
            }
            return;
        }
        public static IRestResponse JiraAPICall(string url, Method meth, string body = "default", string filepath = "default", Dictionary<string, string> headers = null)
        {
            var client = new RestClient(url);
            client.Authenticator = new HttpBasicAuthenticator("jmohan@raincityhousing.org", "jxLdcYQpfcITm9OJwx7m7898");
            //client.Authenticator = new HttpBasicAuthenticator("software@raincityhousing.org", "CgS0A7huPci23M2yBGBq76AF");
            var request = new RestRequest(meth);
            if (body != "default")
            {
                request.AddJsonBody(body);
                request.RequestFormat = DataFormat.Json;
            }
            if (filepath != "default")
            {
                request.AlwaysMultipartFormData = true;
                request.AddFile("file", filepath);
            }
            if (headers != null)
            {
                foreach (var x in headers)
                {
                    request.AddHeader(x.Key, x.Value);
                }
            }
            IRestResponse ret = client.Execute(request);
            return ret;
        }
    }
}
