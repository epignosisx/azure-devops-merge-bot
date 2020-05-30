import * as React from "react";
import { getPolicies, getCurrentRepo, Policy } from "./service";
import { Header, TitleSize } from "azure-devops-ui/Header";
import { IHeaderCommandBarItem } from "azure-devops-ui/HeaderCommandBar";
import { Page } from "azure-devops-ui/Page";
import { ZeroData, ZeroDataActionType } from "azure-devops-ui/ZeroData";

import "./App.scss";
import { CreateMergePolicyPanel } from "./CreateMergePolicy";
import { PolicyTable } from "./PolicyTable";

export const App: React.SFC = ({ children }) => {

    const [isPanelOpened, setIsPanelOpen] = React.useState(false);
    const [repo, setRepo] = React.useState("");
    const [policies, setPolicies] = React.useState<Policy[] | null>(null);

    const loadPolicies = () => {
        if (repo) {
            getPolicies(repo).then(setPolicies).catch(reason => {
                console.log(reason);
                setPolicies([]);
            });
        }
    }

    React.useEffect(() => {
        getCurrentRepo().then(repo => setRepo(repo!.id));
    }, []);

    React.useEffect(loadPolicies, [repo]);

    const item: IHeaderCommandBarItem = {
        id: "add",
        isPrimary: true,
        text: "New Policy",
        onActivate: () => setIsPanelOpen(true)
    };

    return (
        <Page className="mb-page bolt-page-grey">
            <Header 
                title="Merge Policies"
                commandBarItems={[item]}
                titleSize={TitleSize.Large}
            />

            <CreateMergePolicyPanel isOpen={isPanelOpened} setIsOpen={setIsPanelOpen} repo={repo} refresh={loadPolicies} />

            <div className="page-content page-content-top flex-column flex-grow flex-noshrink rhythm-vertical-16">
                {policies && policies.length === 0 && (
                    <ZeroData
                        primaryText="No policies defined yet"
                        secondaryText={
                            <>
                                <div>Create one to get started!</div>
                                <div><small>If this is the first time using Merge-a-Bot make sure to <a href="https://github.com/epignosisx/azure-devops-merge-bot/blob/master/docs/webhook-setup.md" target="_blank">set up the webhook.</a></small></div>
                            </>
                        }
                        actionText="Create New Policy"
                        actionType={ZeroDataActionType.ctaButton}
                        onActionClick={() => setIsPanelOpen(true)}
                        imagePath="https://cdn.vsassets.io/ext/ms.vss-code-web/tags-view-content/Content/no-results.YsM6nMXPytczbbtz.png"
                        imageAltText=""
                    />
                )}
                {policies && policies.length > 0 && (
                    <PolicyTable policies={policies} refresh={loadPolicies} />
                )}
            </div>

        </Page>
    );
};

