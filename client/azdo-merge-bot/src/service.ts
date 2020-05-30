import * as SDK from "azure-devops-extension-sdk";
import { GitServiceIds, IVersionControlRepositoryService, } from "azure-devops-extension-api/Git/GitServices";
import { CommonServiceIds, IExtensionDataService } from "azure-devops-extension-api";
import { GitRepository } from "azure-devops-extension-api/Git/Git";

export const PolicyStrategy = {
    simple: "SpecificSourceAndTargetPolicy",
    cascadingRelease: "ReleaseBranchCascadingPolicy"
};

export type Policy = {
    id?: string;
    createDate: string;
    repositoryId: string;
    strategy: string;
    source: string;
    target: string;
};

export function createPolicy(strategy: string, repoId: string, source: string, target: string): Promise<Policy> {
    if (strategy == PolicyStrategy.cascadingRelease) {
        source = "release/*";
    }
    const context = SDK.getExtensionContext();
    const org = SDK.getHost().name;
    return SDK.getAccessToken().then(token => {
        return SDK.getService<IExtensionDataService>(CommonServiceIds.ExtensionDataService).then(dataService => {
            return dataService.getExtensionDataManager(context.publisherId + "." + context.extensionId, token).then(mng => {
                return mng.createDocument(getCollectionName(repoId), { strategy: strategy, repositoryId: repoId, source: source, target: target, createDate: new Date().toISOString() }).then(doc => {
                    clearCache(org, repoId);
                    return doc;
                });
            });
        });
    });
}

export function deletePolicy(policy: Policy) {
    const context = SDK.getExtensionContext();
    const org = SDK.getHost().name;
    return SDK.getAccessToken().then(token => {
        return SDK.getService<IExtensionDataService>(CommonServiceIds.ExtensionDataService).then(dataService => {
            return dataService.getExtensionDataManager(context.publisherId + "." + context.extensionId, token).then(mng => {
                clearCache(org, policy.repositoryId);
                return mng.deleteDocument(getCollectionName(policy.repositoryId), policy.id!);
            });
        });
    });
}

export function getCurrentRepo(): Promise<GitRepository | null> {
    return SDK.getService<IVersionControlRepositoryService>(GitServiceIds.VersionControlRepositoryService).then(repoSrv => repoSrv.getCurrentGitRepository());
}

export function getPolicies(repo: string): Promise<Policy[]> {
    var context = SDK.getExtensionContext();
    return SDK.getAccessToken().then(token => {
        return SDK.getService<IExtensionDataService>(CommonServiceIds.ExtensionDataService).then(dataService => { 
            return dataService.getExtensionDataManager(getExtensionId(context), token).then(mng => { 
                return mng.getDocuments(getCollectionName(repo)).then((docs: Policy[]) => { 
                    docs.sort((l, r) => {
                        const ld = new Date(l.createDate!);
                        const rd = new Date(r.createDate!);
                        return ld.getTime() - rd.getTime();
                    });
                    return docs;
                });
            });
        });
    });
}

function getCollectionName(repoId: string) {
    return "MergePolicies-" + repoId;
}

function getExtensionId(context: SDK.IExtensionContext) {
    return context.publisherId + "." + context.extensionId
}

function clearCache(org: string, repoId: string): Promise<Response> {
    return fetch(`https://merge-a-bot.azurewebsites.net/policies?organization=${encodeURIComponent(org)}&repositoryId=${encodeURIComponent(repoId)}`, {
        method: "DELETE"
    });
}