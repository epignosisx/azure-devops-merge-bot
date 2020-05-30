# Webhook Setup

Merge-a-Bot needs to listen for branch changes in order to run merge policies. This page guides you through the steps to configure the webhook.

1. Generate a Personal Access Token for the Merge-a-Bot backend to access your Azure DevOps project. Pull requests will be created by the user of the PAT, so it is prefered to have a service account to represent Merge-a-Bot to clearly identify these PRs from normal users of your project. The PAT must have the following permissions: 

   - Code (Read & Write)
   - Extension Data (Read & Write)

    For more information about why this permissions are needed [visit the required permissions page.](.)

2. Perform an HTTP POST to https://something.io/jwt with the PAT and save the response token.

    PowerShell:

    ```ps
    (Invoke-WebRequest -Uri "https://something.io/jwt" -Method "Post" -Body "some-personal-access-token").RawContent
    ```

    curl:

    ```bash
    curl -d "some-personal-access-token" https://something.io/jwt
    ```

3. Go to Project Settings > Service Hooks:

![service hooks](images/service-hooks.png?raw=true)