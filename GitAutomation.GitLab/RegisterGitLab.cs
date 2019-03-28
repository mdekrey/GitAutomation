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
using GitAutomation.Auth;
using GitAutomation.Mvc;

namespace GitAutomation.GitLab
{
    public class RegisterGitLab : IRegisterAuthentication, IRegisterGitServiceApi
    {
        public void RegisterAuthentication(IServiceCollection services, AuthenticationBuilder authBuilder, IConfiguration configuration)
        {

            var authenticationOptions = configuration.Get<Auth.AuthenticationOptions>();
            authBuilder
                .AddOAuth(Auth.Constants.AuthenticationScheme, options =>
                {
                    configuration.GetSection("oauth").Bind(options);
                    var gitLabUrl = configuration["GitLabUrl"].TrimEnd('/');
                    options.AuthorizationEndpoint =  gitLabUrl + "/oauth/authorize";
                    options.TokenEndpoint = gitLabUrl + "/oauth/token";
                    options.Scope.Add("openid");
                    options.Scope.Add("read_user");
                    options.UserInformationEndpoint = gitLabUrl + "/api/v4/user";
                    options.CallbackPath = new PathString("/custom-oauth-signin");
                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id", ClaimValueTypes.Integer);
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
                    options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "name");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "public_email", ClaimValueTypes.Email);
                    options.ClaimActions.MapJsonKey(ClaimTypes.Uri, "web_url");
                    options.BackchannelHttpHandler = new HttpClientHandler();
                    if (Convert.ToBoolean(configuration["BypassSSL"]))
                    {
                        (options.BackchannelHttpHandler as HttpClientHandler).ServerCertificateCustomValidationCallback = delegate { return true; };
                    }
                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            // Get the GitLab user
                            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint + "?access_token=" + context.AccessToken);
                            //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
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
            services.AddSingleton<GitLabServiceApi>();
            services.AddSingleton<IGitServiceApi>(sp => sp.GetRequiredService<GitLabServiceApi>());
            services.Configure<GitLabServiceApiOptions>(configuration.GetSection("gitlab"));
        }
    }
}
