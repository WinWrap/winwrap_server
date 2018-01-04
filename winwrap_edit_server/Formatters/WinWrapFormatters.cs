using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace winwrap_edit_server.Formatters
{
    public class WinWrapBasicInputFormatter : TextInputFormatter
    {
        public WinWrapBasicInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/winwrap"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanReadType(Type type)
        {
            return type == typeof(WinWrapMessage);
        }

        async public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            var request = context.HttpContext.Request;
            using (var reader = new StreamReader(request.Body, encoding))
            {
                var text = await reader.ReadToEndAsync();
                var content = new WinWrapMessage(text);
                return await InputFormatterResult.SuccessAsync(content);
            }
        }
    }

    public class WinWrapBasicOutputFormatter : TextOutputFormatter
    {
        public WinWrapBasicOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/winwrap"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanWriteType(Type type)
        {
            return type == typeof(WinWrapMessage);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            IServiceProvider serviceProvider = context.HttpContext.RequestServices;
            var logger = serviceProvider.GetService(typeof(ILogger<WinWrapBasicOutputFormatter>)) as ILogger;
            var text = (context.Object as WinWrapMessage).ToString();
            logger.LogInformation($"WinWrapBasicMessage-Response: {text}");
            var response = context.HttpContext.Response;
            return response.WriteAsync(text, selectedEncoding);
        }
    }
}
