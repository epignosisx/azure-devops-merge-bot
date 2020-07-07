# Troubleshooting

**Q: I followed the steps in [webhook setup](https://github.com/epignosisx/azure-devops-merge-bot/blob/master/docs/webhook-setup.md), yet when a branch is updated, it does not create a PR automatically.**

1. Regenerate the Merge-a-Bot auth token and update the webhook. Make sure the *HTTP Headers* is correctly formatted: 
   ```
   Authorization: Bearer my-bearer-token-goes-here-all-in-one-line-just-spaces
   ```
   
2. Verify PAT permissions are correct as [described here](https://github.com/epignosisx/azure-devops-merge-bot/blob/master/docs/pat-permissions.md).
3. Verify user behind the PAT has the permissions to the Project containing the repositories, as well as the repositories itself.
4. Verify merge policies are correctly configured. Look for typos in the branch names.

**Q: The PR is created, yet it is not merged once all checks complete.**

Verify the user behind the PAT has the permission to "Bypass Policy when completing pull requests" as described in the [webhook setup](https://github.com/epignosisx/azure-devops-merge-bot/blob/master/docs/webhook-setup.md) guide.
