# GitLab Setup for Authentication/Authorization

1. Head to your Applications page on your gitlab app, at a URL like https://gitlab.example.com/profile/applications to create GitAuto as an application.

	Name: GitAuto
	RedirectUri: https://gitauto.example.com/custom-oauth-signin
	Scopes: read_user, openid

2. Add these settings in your configuration.json for GitLab, replacing `APPLICATION_ID` and `SECRET` with the settings from your GitLab application page:

		  "authentication": {
			"type": "GitAutomation.GitLab.RegisterGitLab, GitAutomation.GitLab",
			"BypassSSL": false,
			"GitLabUrl": "https://gitlab.example.com",
			"oauth": {
			  "clientId": "APPLICATION_ID",
			  "clientSecret": "SECRET"
			}
		  },

	*Note*: If you use self-signed certificates, set `"BypassSSL": true`.

3. Make sure the GitLab addon is configured for your docker deployment.

