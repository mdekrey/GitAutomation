# GitHub Setup

There are a few parts of GitAutomation that can be used independently with
GitHub.

* Git repository hosting and Pull Requests
* Authentication/Authorization

You may use each of these parts separately, such as a GitHub repository with an
ADFS auth system, or a VSO repository with a GitHub auth system.

## Git Repository Hosting and Pull Requests

The section of the configuration corresponds to this:

    "git": {
      "repository": "https://mdekrey@github.com/mdekrey/git-branching",
      "password": "YOUR_GITHUB_PASSWORD",
      "apiType": "GitAutomation.GitHub.RegisterGitHub, GitAutomation.GitHub",
      "userEmail": "git.automation@example.com",
      "userName": "Git Automation"
    },

This breaks down as follows:

* *repository* - The repository URL to clone, including the username.
* *password* - This can be your github password, but I do not recommend it.
  Instead, set up a [personal access token](https://github.com/settings/tokens)
  for yourself. Don't forget to also put it in `/git-credentials.txt`.
* *apiType* - For GitHub, this should be `GitAutomation.GitHub.RegisterGitHub,
  GitAutomation.GitHub`.
* *userEmail* - The email you want automated merge commits to be recorded as. If
  you want to control the image in GitHub for this, make sure the email is
  registered to a valid GitHub user with that profile picture.
* *userName* - The name you want automated merge commits to be recorded as.

## Authentication/Authorization

Start by setting up a [GitHub OAuthApp](https://github.com/settings/developers).

The sample has this as the configuration for authentication:

    "authentication": {
      "type": "GitAutomation.GitHub.RegisterGitHub, GitAutomation.GitHub",
      "oauth": {
        "clientId": "YOUR_CLIENT_ID",
        "clientSecret": "YOUR_CLIENT_SECRET"
      }
    },

* *type* - For GitHub, this should be `GitAutomation.GitHub.RegisterGitHub,
  GitAutomation.GitHub`.
* *oauth.clientId* - The Client ID provided by GitHub.
* *oauth.clientSecret* - The Client Secret provided by GitHub.

For the authorization, this gets a little more complex; a separate document will
be provided for the available roles and such. An example is below; replace the
username with your own to gain access.

    "authorization": {
      "roles": {
        "administrator": [
          {
            "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
            "value": "mdekrey"
          }
        ],
        "read": [
          { "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" }
        ]
      }
    }

In brief, unauthorized users will be shown the claims they receive; if any one
matches the rules provided in your config, they will receive that role. Omitted
values are not matched against.
