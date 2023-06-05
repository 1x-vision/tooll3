using System;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace T3.Operators.Types.Id_5cc6cc51_75ab_474d_b0b8_aaa03ea77326
{
    public class RequestUrl : Instance<RequestUrl>
    {
        [Output(Guid = "D6DACC5B-726E-48C4-A6CD-76C0581DB809")]
        public readonly Slot<string> Result = new();

        public RequestUrl()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Download2();
        }

        private async void Download2()
        {
            try
            {
                var client = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
                var request = new HttpRequestMessage
                                  {
                                      RequestUri = new Uri("https://oyjobypyozkufrbrmqnt.supabase.co/rest/v1/scores?select=data"),
                                      Method = HttpMethod.Get,
                                  };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("apikey",
                                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im95am9ieXB5b3prdWZyYnJtcW50Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2ODAwMDA4MTUsImV4cCI6MTk5NTU3NjgxNX0.9Km2SmPnOQre_obeKr_DSr2l33PECtMWIF1VuxE1zck");
                request.Headers.Add("Authorization",
                                    "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im95am9ieXB5b3prdWZyYnJtcW50Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2ODAwMDA4MTUsImV4cCI6MTk5NTU3NjgxNX0.9Km2SmPnOQre_obeKr_DSr2l33PECtMWIF1VuxE1zck");
                var httpResponseTask = await client.SendAsync(request);
                
                Result.Value =await httpResponseTask.Content.ReadAsStringAsync();;
                Result.DirtyFlag.Invalidate();
            }
            catch (Exception e)
            {
                Log.Warning("Download failed: " + e.Message);
            }
        }
        
        [Input(Guid = "103d0f2c-343d-488a-baf8-5ffab97d0b42")]
        public readonly InputSlot<string> Url = new();
    }
}