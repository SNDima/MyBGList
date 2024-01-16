using Asp.Versioning;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace MyBGList_ApiVersion.Controllers.v2
{
	[Route("v{version:apiVersion}/[controller]/[action]")]
	[ApiController]
	[ApiVersion("2.0")]
	public class CodeOnDemandController : ControllerBase
	{
		[HttpGet(Name = "Test")]
		[EnableCors("AnyOrigin_GetOnly")]
		[ResponseCache(NoStore = true)]
		public ContentResult Test()
		{
			return Content("<script>" +
				"window.alert('Your client supports JavaScript!" +
				"\\r\\n\\r\\n" +
				$"Server time (UTC): {DateTime.UtcNow:o}" +
				"\\r\\n" +
				"Client time (UTC): ' + new Date().toISOString());" +
				"</script>" +
				"<noscript>Your client does not support JavaScript</noscript>",
				"text/html");
		}

		[HttpGet(Name = "Test2")]
		[EnableCors("AnyOrigin_GetOnly")]
		[ResponseCache(NoStore = true)]
		public ContentResult Test2(int addMinutes = 0)
		{
			return Content("<script>" +
				"window.alert('Your client supports JavaScript!" +
				"\\r\\n\\r\\n" +
				$"Server time (UTC): {DateTime.UtcNow.AddMinutes(addMinutes):o}" +
				"\\r\\n" +
				"Client time (UTC): ' + new Date().toISOString());" +
				"</script>" +
				"<noscript>Your client does not support JavaScript</noscript>",
				"text/html");
		}
	}
}
