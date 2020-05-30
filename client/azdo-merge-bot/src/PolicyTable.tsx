import * as React from "react";
import { Policy, deletePolicy, PolicyStrategy } from "./service";
import { Card } from "azure-devops-ui/Card";
import { Table, renderSimpleCell, renderSimpleCellValue, TableColumnLayout, ColumnMore, ITableColumn } from "azure-devops-ui/Table";
import { ArrayItemProvider } from "azure-devops-ui/Utilities/Provider";
import { IMenuItem } from "azure-devops-ui/Menu";
import "./PolicyTable.scss";

export interface PolicyTableProps {
    policies: Policy[];
    refresh: () => void;
}

export const PolicyTable: React.SFC<PolicyTableProps> = (props) => {

    const [rowIndex, setRowIndex] = React.useState(-1);

    const onMenuItemActivated = (menuItem: IMenuItem, event: any) => {
        if (menuItem.id === "delete") {
            const policy = props.policies[rowIndex];
            deletePolicy(policy).then(props.refresh);
            setRowIndex(-1);
        }
    };

    const onActivate = (rowIndex: number, columnIndex: number) => {
        setRowIndex(rowIndex);
    };

    function renderStrategyCell(rowIndex: number, columnIndex: number, tableColumn: ITableColumn<any>, tableItem: any, ariaRowIndex: number) {
        let value = tableItem[tableColumn.id];
        if (tableItem[tableColumn.id] === PolicyStrategy.simple) {
            value = "Source to Target";
        } else if (tableItem[tableColumn.id] === PolicyStrategy.cascadingRelease) {
            value = "Cascade release branches to target branch"
        }
        return renderSimpleCellValue(columnIndex, tableColumn, value, ariaRowIndex);
    }

    const fixedColumns: any = [
        {
            id: "strategy",
            name: "Strategy",
            readonly: true,
            renderCell: renderStrategyCell,
            width: -20
        },
        {
            columnLayout: TableColumnLayout.none,
            id: "source",
            name: "Source Branch",
            readonly: true,
            renderCell: renderSimpleCell,
            width: -25
        },
        {
            columnLayout: TableColumnLayout.none,
            id: "target",
            name: "Target Branch",
            readonly: true,
            renderCell: renderSimpleCell,
            width: -25
        },
        new ColumnMore(() => {
            return {
                onActivate: onMenuItemActivated,
                id: "sub-menu",
                items: [
                    { id: "delete", text: "Delete" }
                ]
            };
        }, undefined, onActivate)
    ];

    const provider = new ArrayItemProvider<Policy>(props.policies);

    return (
        <Card className="bolt-table-card mb-card-white" contentProps={{ contentPadding: false }}>
            <Table columns={fixedColumns} itemProvider={provider} role="table" />
        </Card>
    );
};