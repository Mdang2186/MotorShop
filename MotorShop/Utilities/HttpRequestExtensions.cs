namespace MotorShop.Utilities
{
    public static class HttpRequestExtensions
    {
        public static string PathAndQuery(this HttpRequest request)
        {
            return request.Path + request.QueryString;
        }
    }
}