# Personal Access Token Permissions

The extension's backend needs to interact with the Azure DevOps API once it is notified that branches changed. These are the pemissions needed:

1. **Code (Read & Write)**. Needed in order to get branches, create pull requests, and merge pull requests.

    It's very important to note that the repository code is never requested. It's not needed. The extension just cares about branch names, commit ids, and pull requests with no regard for the code in them. Unfortunately, Azure DevOps does not have more granular permissions to just request the repo metadata without access to the code.

2. **Extension Data (Read & Write)**. The merge policies are stored in the Azure DevOps Extension Data API (this is a simple document store). This means that the extension does not store any of your information in its database, in fact, it doesn't have a database at all, but it does require access to store and retrieve the merge policies from the Azure DevOps API.
