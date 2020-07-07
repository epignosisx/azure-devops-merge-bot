# Webhook Setup

Merge-a-Bot needs to listen for branch changes in order to run merge policies. This page guides you through the steps to configure the webhook.

1. Generate a Personal Access Token (PAT) for the Merge-a-Bot backend to access your Azure DevOps project. Pull requests will be created and merged by the user of the PAT, so it is prefered to have a service account to represent Merge-a-Bot to clearly identify these PRs from normal users of your project. The PAT must have the following permissions: 

   - Code (Read & Write)
   - Extension Data (Read & Write)

    For more information about why these permissions are needed [visit the required permissions page.](https://github.com/epignosisx/azure-devops-merge-bot/blob/master/docs/pat-permissions.md)

    Ensure the user behind the PAT has access to the Project and Repositories.

    Note, if pull requests have Required Reviewers, then Merge-a-Bot will not be able to automatically merge pull requests that succeed (pass all the checks). This can be fixed if the user behind the PAT has the permission to "Bypass policies when completing pull requests" for all the repos or the specific repos that will use the extension:

    ![repo bypass policies](images/repo-bypass-policies.png?raw=1)

2. Next, you will generate the Merge-a-Bot Auth token. Perform a HTTP POST to https://merge-a-bot.azurewebsites.net/jwt with the PAT and save the Merge-a-Bot token.

    PowerShell:

    ```ps
    (Invoke-WebRequest -Uri "https://merge-a-bot.azurewebsites.net/jwt" -Method "Post" -Body "some-personal-access-token").RawContent
    ```

    curl:

    ```bash
    curl -d "some-personal-access-token" https://merge-a-bot.azurewebsites.net/jwt
    ```

3. Go to Project Settings > Service Hooks:

![service hooks](images/service-hooks.png?raw=true)

4. Click on the Plus icon, look for the "Web Hooks" option, and click "Next":

![new hook](images/new-hook.png?raw=true)

5. For "Trigger on this type of event" select "Code Pushed" and click "Next".

![hook trigger](images/hook-trigger.png?raw=true)

6. In the Action step, for "URL" enter `https://merge-a-bot.azurewebsites.net/webhook` and for HTTP headers enter `Authorization: Bearer {merge-a-bot-token}`. This is the token generated in step 2, not the Azure DevOps PAT! For "Resource details to send" keep the default option of "All". Set "Messages to send" and "Detailed messages to send" to "None".

![hook settings](images/hook-settings.png?raw=true)

7. Click Test and Finish to complete.
