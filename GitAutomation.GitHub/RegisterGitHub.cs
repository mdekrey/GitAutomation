using GitAutomation.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using GitAutomation.GitService;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace GitAutomation.GitHub
{
    public class RegisterGitHub : IRegisterGitServiceApi, IRegisterAuthentication
    {
        public void RegisterAuthentication(IServiceCollection services, AuthenticationBuilder authBuilder, IConfiguration configuration)
        {
            var authenticationOptions = configuration.Get<Plugins.AuthenticationOptions>();
            authBuilder
                .AddOAuth(authenticationOptions.Scheme, options =>
                {
                    configuration.GetSection("oauth").Bind(options);
                    options.CallbackPath = new PathString("/custom-oauth-signin");
                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
                    options.ClaimActions.MapJsonKey("urn:github:name", "name");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
                    options.ClaimActions.MapJsonKey("urn:github:url", "url");
                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            // Get the GitHub user
                            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                            context.RunClaimActions(user);
                        }
                    };
                });
        }

        public void RegisterGitServiceApi(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IGitServiceApi, GitHubServiceApi>();
        }
    }
}
