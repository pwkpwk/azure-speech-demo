namespace TextTranslation.Demo.Core
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Use: TextTranslation.Demo.Core \"{phrase}\" {key} {target locale 1} {target locale 2}...{target locale N}");
            }
            else
            {
                StringBuilder query = new StringBuilder("api-version=3.0");
                for (int i = 2; i < args.Length; ++i)
                {
                    query.AppendFormat("&to={0}", args[i]);
                }
                UriBuilder ub = new UriBuilder("https", "api.cognitive.microsofttranslator.com")
                {
                    Path = "translate",
                    Query = query.ToString()
                };

                using (HttpClient client = new HttpClient())
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ub.Uri))
                {
                    JToken token = JToken.FromObject(new RequestItem[] { new RequestItem() { Text = args[0] } });
                    request.Content = new StringContent(token.ToString(Formatting.None), Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", args[1]);

                    using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        HttpResponseMessage response = client.SendAsync(request, cts.Token).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            Task<Stream> t = response.Content.ReadAsStreamAsync();
                            t.Wait(cts.Token);
                            using (JsonReader reader = new JsonTextReader(new StreamReader(t.Result, Encoding.UTF8)))
                            {
                                JToken jt = JToken.ReadFrom(reader);
                                Console.Out.WriteLine(jt.ToString());
                            }
                        }
                    }
                }
            }
        }

        [JsonObject]
        private sealed class RequestItem
        {
            [JsonProperty(PropertyName = "Text", Required = Required.Always)]
            public string Text { get; set; }
        }
    }
}
