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

# GitLab Setup for git services

1. Get the Project ID number, visible on your project dashboard underneath the name. You can verify you got the right ID by visiting https://gitlab.example.com/projects/PROJECT_ID.
2. Create an account specifically for GitAutomation. This will flag the work it does as a separate account in GitLab. It needs "Maintainer" permissions for your project.
3. As that account, generate a personal access token at https://gitlab.example.com/profile/personal_access_tokens. It needs `api` scope. Save it for use in the next step.
4. Change your `git` configuration in the `configuration.json`, using "USERNAME" as the username of the GitAutomation account and "PERSONAL_ACCESS_TOKEN" for the personal access token.

		  "git": {
			"repository": "https://gitlab-ci-token:PERSONAL_ACCESS_TOKEN@gitlab.example.com/namespace/project.git",
			"apiType": "GitAutomation.GitLab.RegisterGitLab, GitAutomation.GitLab",
			"userEmail": "git.automation@example.com",
			"userName": "Git Automation",
			"checkoutPath": "/working",
			"gitlab": {
			  "projectId": PROJECT_ID,
			  "username": "USERNAME",
			  "personalAccessToken": "PERSONAL_ACCESS_TOKEN"
			}
		  },

	*Note*: Self-signed certificates are not supported with the git integration at this time.
