import * as React from 'react';
import { Panel } from "azure-devops-ui/Panel";
import { RadioButton, RadioButtonGroup } from "azure-devops-ui/RadioButton";
import { FormItem } from "azure-devops-ui/FormItem";
import { TextField, TextFieldWidth } from "azure-devops-ui/TextField";
import { PolicyStrategy, createPolicy, getCurrentRepo } from './service';

export interface CreateMergePolicyPanelProps {
    isOpen: boolean;
    setIsOpen: (isOpen: boolean) => void;
    repo: string;
    refresh: () => void;
}

export const CreateMergePolicyPanel: React.SFC<CreateMergePolicyPanelProps> = (props) => {
    const [strategy, setStrategy] = React.useState(PolicyStrategy.simple);
    const [sourceBranch, setSourceBranch] = React.useState("");
    const [targetBranch, setTargetBranch] = React.useState("");
    const [submitted, setSubmitted] = React.useState(false);

    const sourceBranchError = submitted && sourceBranch.length === 0;
    const targetBranchError = submitted && targetBranch.length == 0;

    function onStrategyChanged(buttonId: string) {
        if (buttonId !== strategy) {
            setStrategy(buttonId);
            setTargetBranch("");
            setSourceBranch("");
        }
    }

    function resetForm() {
        setStrategy(PolicyStrategy.simple);
        setSourceBranch("");
        setTargetBranch("");
        setSubmitted(false);
    }

    function onClose() {
        resetForm();
        props.setIsOpen(false);
    }

    function onCreate() {
        setSubmitted(true);
        if (targetBranch.length === 0) {
            return;
        }
        if (strategy === PolicyStrategy.simple && sourceBranch.length === 0) {
            return;
        }
        createPolicy(strategy, props.repo, sourceBranch, targetBranch).then(() => {
            resetForm();
            props.refresh();
            props.setIsOpen(false);
        });
    }
    
    return (
        <>
            {props.isOpen && (
            <Panel 
                titleProps={{text: "Create Merge Policy"}} 
                onDismiss={onClose}
                footerButtonProps={[
                    { text: "Cancel", onClick: onClose },
                    { text: "Create", primary: true, onClick: onCreate }
                ]}
            >
                <div className="flex-column">
                    <FormItem label="Strategy">
                        <RadioButtonGroup onSelect={onStrategyChanged} selectedButtonId={strategy}>
                            <RadioButton id={PolicyStrategy.simple} text="From a source to a target branch" key="simple" ariaDescribedBy="simple-desc" />
                            <div className="mb-policy-desc" id="simple-desc">Changes to source branch will be merged to target branch.</div>
                            
                            <RadioButton id={PolicyStrategy.cascadingRelease} text="Cascade release branches to a target branch" key="cascading-release" className="mb-mt-8" ariaDescribedBy="cascading-release-desc" />
                            <div className="mb-policy-desc" id="cascading-release-desc">
                                <div>Changes to branches with the pattern release/* are merged to other release/* branches and finally to a default branch following <a href="https://semver.org/" target="_blank">SemVer 2.</a></div>
                                <div className="mb-mt-4">Ex: A change to release/2.0 will be merged to release/2.1, then release/2.1 will be merged down to the default branch (master, develop, etc), but not to release/1.0</div>
                            </div>
                        </RadioButtonGroup>
                    </FormItem>

                    {strategy == PolicyStrategy.simple && (
                        <>
                            <FormItem 
                                label="Source Branch" 
                                error={sourceBranchError}
                                message={sourceBranchError ? "Source branch is required" : undefined}
                                className="mb-mt-16"
                            >
                                <TextField
                                    value={sourceBranch}
                                    onChange={(e, newValue) => setSourceBranch(newValue)}
                                    width={TextFieldWidth.standard}
                                />
                            </FormItem>
                            <FormItem 
                                label="Target Branch" 
                                error={targetBranchError}
                                message={targetBranchError ? "Target branch is required" : undefined}
                                className="mb-mt-16"
                            >
                                <TextField
                                    value={targetBranch}
                                    onChange={(e, newValue) => setTargetBranch(newValue)}
                                    width={TextFieldWidth.standard}
                                />
                            </FormItem>
                        </>
                    )}
                    {strategy == PolicyStrategy.cascadingRelease && (
                        <FormItem 
                            label="Default Branch" 
                            error={targetBranchError}
                            message={targetBranchError ? "Default branch is required" : undefined}
                            className="mb-mt-16"
                        >
                            <TextField
                                value={targetBranch}
                                onChange={(e, newValue) => setTargetBranch(newValue)}
                                width={TextFieldWidth.standard}
                            />
                        </FormItem>
                    )}
                </div>
            </Panel>
            )}
        </>
    );
};