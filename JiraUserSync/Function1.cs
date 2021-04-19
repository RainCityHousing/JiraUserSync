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
        //    public static void Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer, ILogger log)
            {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            //dicts to hold customer / org customer lists
            Dictionary<string, string> custListDict = getAllMembers("/rest/servicedeskapi/servicedesk/OP/customer");
            Dictionary<string, string> orgListDict = getAllMembers("/rest/servicedeskapi/organization/3/user");

            log.LogInformation("Customer list size is: " + custListDict.Count);
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
               // var re = JiraAPICall("https://raincityhousing.atlassian.net/rest/servicedeskapi/organization/3/user", Method.DELETE, body);
            }
            return;
        }
        public static Dictionary<string,string> getAllMembers(string url)
        {
            string furl = "https://raincityhousing.atlassian.net" + url;
            Dictionary<string, string> retList = new Dictionary<string, string>();
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("X-ExperimentalAPI", "opt-in");
            int currentIndex = 0;
            var firstcall = JiraAPICall(furl, Method.GET, "default", headers);
            CustomerList CustList = JsonConvert.DeserializeObject<CustomerList>(firstcall.Content);
            foreach (var y in CustList.Values)
                retList.Add(y.AccountId, y.EmailAddress);
            currentIndex = CustList.Start + CustList.Size;
            while (CustList.IsLastPage == false)
            {
                var secondCall = JiraAPICall(furl + "?start=" + currentIndex, Method.GET, "default", headers);
                CustList = JsonConvert.DeserializeObject<CustomerList>(secondCall.Content);
                foreach (var y in CustList.Values)
                {
                    if (y.EmailAddress != null && y.EmailAddress != "")
                        if (y.Active)
                            retList.Add(y.AccountId, y.EmailAddress);
                }
                currentIndex = CustList.Start + CustList.Size;
            }
            return retList;
        }
        public static IRestResponse JiraAPICall(string url, Method meth, string body = "default", Dictionary<string, string> headers = null)
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
