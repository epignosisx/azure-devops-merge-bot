import "azure-devops-ui/Core/override.css";
import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";
import * as ReactDOM from "react-dom";
import { App } from "./App";

SDK.init().then(() => {
    ReactDOM.render(<App />, document.getElementById("root"));
}).catch(reason => {
    console.log(reason);
    document.getElementById("root")!.textContent = "Failed to load extension :(";
});