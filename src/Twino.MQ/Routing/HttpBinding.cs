using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Twino.MQ.Clients;
using Twino.Protocols.TMQ;

namespace Twino.MQ.Routing
{
    /// <summary>
    /// HTTP Binding.
    /// Targets Endpoints.
    /// Message is send as Querystring for GET and DELETE methods.
    /// For other methods sends as JSON body.
    /// </summary>
    public class HttpBinding : Binding
    {
        /// <summary>
        /// Creates new HTTP binding.
        /// Name is the name of the binding.
        /// Target should be the endpoint.
        /// Priority for router binding.
        /// </summary>
        public HttpBinding(string name, string target, HttpBindingMethod method, int priority, BindingInteraction interaction)
            : base(name, target, (ushort) method, priority, interaction)
        {
        }

        /// <summary>
        /// Sends message to target as HTTP request and waits for response.
        /// Ok Accepted and Created responses return true, others return false
        /// </summary>
        public override Task<bool> Send(MqClient sender, TmqMessage message)
        {
            HttpClient client = new HttpClient();

            HttpBindingMethod method = (HttpBindingMethod) ContentType;
            string content = message.Length > 0 ? message.ToString() : null;
            Task<HttpResponseMessage> response = null;

            switch (method)
            {
                case HttpBindingMethod.Get:
                {
                    string uri = Target;
                    if (!string.IsNullOrEmpty(content))
                        uri += "?" + content;

                    response = client.GetAsync(uri);
                    break;
                }

                case HttpBindingMethod.Delete:
                {
                    string uri = Target;
                    if (!string.IsNullOrEmpty(content))
                        uri += "?" + content;

                    response = client.DeleteAsync(uri);
                    break;
                }

                case HttpBindingMethod.Post:
                    response = client.PostAsync(Target, new StringContent(content, Encoding.UTF8, "application/json"));
                    break;

                case HttpBindingMethod.Put:
                    response = client.PutAsync(Target, new StringContent(content, Encoding.UTF8, "application/json"));
                    break;

                case HttpBindingMethod.Patch:
                    response = client.PatchAsync(Target, new StringContent(content, Encoding.UTF8, "application/json"));
                    break;
            }

            if (response == null)
                return Task.FromResult(false);

            return ProcessResponse(response);
        }

        /// <summary>
        /// Waits for response and checks response status code.
        /// Ok Accepted and Created responses return true, others return false
        /// </summary>
        private async Task<bool> ProcessResponse(Task<HttpResponseMessage> task)
        {
            try
            {
                HttpResponseMessage response = await task;
                return response != null &&
                       (response.StatusCode == HttpStatusCode.OK ||
                        response.StatusCode == HttpStatusCode.Accepted ||
                        response.StatusCode == HttpStatusCode.Created);
            }
            catch
            {
                return false;
            }
        }
    }
}