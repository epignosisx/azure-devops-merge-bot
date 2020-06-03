# azure-devops-merge-bot
This Azure DevOps extension automates the merging of branches based on policies. 

Say, you want to merge every change that gets to `develop` to `master`: Merge-a-Bot will automate this process, by first creating a pull request, then monitoring the pull request until it is ready to merge and finally merging it.

Currently, there are two policies available:

1. **From a source to a target branch**. Changes to source branch will be merged to target branch.
2. **Cascade release branches to a target branch**. Changes to branches with the pattern release/* are merged to other release/* branches and finally to a default branch following SemVer 2.

    Ex: A change to release/2.0 will be merged to release/2.1, then release/2.1 will be merged down to the default branch (master, develop, etc), but not to release/1.0

## Contribute
Have a feature request? Running into problems? Create an issue!
