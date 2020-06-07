using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quasar.Hub.Hubs;
using Quasar.Hub.Services;
using Quasar.Hub.Settings;

namespace Quasar.Hub
{
	public class Startup
	{
		private readonly IConfiguration _configuration;

		public Startup(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc();

			var twilioSettings = _configuration
				.GetSection("TwilioSettings")
				.Get<TwilioSettings>();
			services.Configure<TwilioSettings>(config =>
			{
				config.AccountSid = twilioSettings.AccountSid;
				config.ApiKey = twilioSettings.ApiKey;
				config.ApiSecret = twilioSettings.ApiSecret;
			});
			
			services.AddSignalR();
			services.AddHealthChecks();
			
			services.AddCors(option =>
			{
				option.AddPolicy("CorsPolicy", builder =>
					builder.WithOrigins("http://localhost:4200", "http://quasarwebapplb-541710245.us-east-2.elb.amazonaws.com")
						.AllowAnyMethod()
						.AllowAnyHeader()
						.AllowCredentials());
			});

			services.AddTransient<IVideoService, VideoService>();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			
			app.UseCors("CorsPolicy");

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				endpoints.MapHub<NotificationHub>("/notificationHub");
				endpoints.MapHealthChecks("/healthcheck", new HealthCheckOptions
				{
					AllowCachingResponses = false,
					ResponseWriter = WriteResponse
				});
			});
		}
        
		private static Task WriteResponse(HttpContext context, HealthReport result)
		{
			context.Response.ContentType = "application/json; charset=utf-8";

			var options = new JsonWriterOptions
			{
				Indented = true
			};

			using (var stream = new MemoryStream())
			{
				using (var writer = new Utf8JsonWriter(stream, options))
				{
					writer.WriteStartObject();
					writer.WriteString("status", result.Status.ToString());
					writer.WriteEndObject();
				}

				string json = Encoding.UTF8.GetString(stream.ToArray());

				return context.Response.WriteAsync(json);
			}
		}
	}
}
