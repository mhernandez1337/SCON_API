namespace WebSupplementalAPI
{
    public class RefererKeyMiddleWare
    {
        private readonly RequestDelegate _next;
        private
        const string REFERER = "Referer";
        public RefererKeyMiddleWare(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(REFERER, out
                    var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized client");
                return;
            }
            var appSettings = context.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = appSettings.GetValue<string>(REFERER);
            if (!apiKey.Equals(extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized client");
                return;
            }
            await _next(context);
        }
    }
}
