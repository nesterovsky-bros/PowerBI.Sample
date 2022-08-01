/*
*  Power BI Visual CLI
*
*  Copyright (c) Microsoft Corporation
*  All rights reserved.
*  MIT License
*
*  Permission is hereby granted, free of charge, to any person obtaining a copy
*  of this software and associated documentation files (the ""Software""), to deal
*  in the Software without restriction, including without limitation the rights
*  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
*  copies of the Software, and to permit persons to whom the Software is
*  furnished to do so, subject to the following conditions:
*
*  The above copyright notice and this permission notice shall be included in
*  all copies or substantial portions of the Software.
*
*  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
*  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
*  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
*  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
*  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
*  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
*  THE SOFTWARE.
*/
"use strict";

import "./../style/visual.less";
import powerbi from "powerbi-visuals-api";
import VisualConstructorOptions = powerbi.extensibility.visual.VisualConstructorOptions;
import VisualUpdateOptions = powerbi.extensibility.visual.VisualUpdateOptions;
import IVisual = powerbi.extensibility.visual.IVisual;
import EnumerateVisualObjectInstancesOptions = powerbi.EnumerateVisualObjectInstancesOptions;
import VisualObjectInstance = powerbi.VisualObjectInstance;
import DataView = powerbi.DataView;
import VisualObjectInstanceEnumeration = powerbi.VisualObjectInstanceEnumeration;

import IVisualHost = powerbi.extensibility.IVisualHost;

import { VisualSettings } from "./settings";


export class Visual implements IVisual {
    private settings: VisualSettings;

    private host: IVisualHost;
    private idInput: HTMLInputElement;
    private checkInput: HTMLInputElement;
    private commentInput: HTMLInputElement;
    private log: HTMLElement;

    constructor(options: VisualConstructorOptions)
    {
        this.host = options.host;

        if (document) 
        {
            const target = options.element;
            const table = document.createElement("table");
            const idRow = document.createElement("tr");
            const checkRow = document.createElement("tr");
            const commentRow = document.createElement("tr");
            const buttonRow = document.createElement("tr");
            const idInput = document.createElement("input");
            const checkInput = document.createElement("input");
            const commentInput = document.createElement("input");
            const button = document.createElement("button");

            idInput.type = "text";
            checkInput.type = "checkbox";
            commentInput.type = "text";
            button.textContent = "Update";

            table.setAttribute("border", "1");

            table.appendChild(idRow);
            table.appendChild(checkRow);
            table.appendChild(commentRow);
            table.appendChild(buttonRow);

            let cell = document.createElement("td");

            cell.textContent = "ID";
            idRow.appendChild(cell);
            cell = document.createElement("td");
            idRow.appendChild(cell);
            cell.appendChild(idInput);

            cell = document.createElement("td");
            cell.textContent = "Check";
            checkRow.appendChild(cell);
            cell = document.createElement("td");
            checkRow.appendChild(cell);
            cell.appendChild(checkInput);
            
            cell = document.createElement("td");
            cell.textContent = "Comment";
            commentRow.appendChild(cell);
            cell = document.createElement("td");
            commentRow.appendChild(cell);
            cell.appendChild(commentInput);

            this.log = cell = document.createElement("td");
            buttonRow.appendChild(cell);
            cell = document.createElement("td");
            buttonRow.appendChild(cell);
            cell.appendChild(button);

            target.appendChild(table);

            this.idInput = idInput;
            this.checkInput = checkInput;
            this.commentInput = commentInput;

            button.addEventListener("click", e => this.updateData(e));
        }
    }

    public update(options: VisualUpdateOptions) {
        this.settings = VisualSettings.parse(options?.dataViews?.[0]);

        let dataView: DataView = options.dataViews[0];

        const id = dataView?.categorical?.values?.[0].single;
        const check = dataView?.categorical?.values?.[1].single;
        const comment = dataView?.categorical?.values?.[2].single;

        this.idInput.value = String(id);
        this.checkInput.checked = !!check;
        this.commentInput.value = String(comment);
    }

    /**
     * This function gets called for each of the objects defined in the capabilities files and allows you to select which of the
     * objects and properties you want to expose to the users in the property pane.
     *
     */
    public enumerateObjectInstances(options: EnumerateVisualObjectInstancesOptions): VisualObjectInstanceEnumeration 
    {
        return VisualSettings.enumerateObjectInstances(this.settings || VisualSettings.getDefault(), options);
    }

    private async updateData(e: Event): Promise<void>
    {
        const id = this.idInput.value;
        const check = this.checkInput.checked;
        const comment = this.commentInput.value;

        this.log.textContent = `Querying...`;

        const response = await fetch("https://localhost:7096/api/Status",
        {
            method: "POST",
            mode: "cors",
            headers: 
            {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ ID: Number(id || 0), check, comment })
        });

        const result = await response.json();

        this.log.textContent = `Result: ${JSON.stringify(result)}`;
    }
}