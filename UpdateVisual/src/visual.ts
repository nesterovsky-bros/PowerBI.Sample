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

import IVisualHost = powerbi.extensibility.visual.IVisualHost;
import VisualEnumerationInstanceKinds = powerbi.VisualEnumerationInstanceKinds;

import { VisualSettings } from "./settings";

export class Visual implements IVisual {
    private settings: VisualSettings;

    private host: IVisualHost;
    private id: any;
    private managerCheckbox: HTMLInputElement;
    private managerComment: HTMLInputElement;
    private clerkCheckbox: HTMLInputElement;
    private clerkComment: HTMLInputElement;
    private robotCheckbox: HTMLInputElement;
    private updateButton: HTMLButtonElement;

    constructor(options: VisualConstructorOptions)
    {
        this.host = options.host;

        if (document) 
        {
            const target = options.element;

            target.innerHTML = template;

            this.managerCheckbox = target.querySelector("#manager-status");
            this.managerComment = target.querySelector("#manager-comment");
            this.clerkCheckbox = target.querySelector("#clerk-status");
            this.clerkComment = target.querySelector("#clerk-comment");
            this.robotCheckbox = target.querySelector("#robot-status");
            this.updateButton = target.querySelector("#update");

            this.updateButton.addEventListener("click", e => this.updateData(e));
        }
    }

    public update(options: VisualUpdateOptions) {
        this.settings = VisualSettings.parse(options?.dataViews?.[0]);

        let dataView: DataView = options.dataViews[0];

        const id = dataView?.categorical?.categories?.[0]?.values?.[0];

        if (id !== this.id)
        {
            const managerCheck = dataView?.categorical?.categories?.[1]?.values?.[0];
            const managerComment = dataView?.categorical?.categories?.[2]?.values?.[0];
            const clerkCheck = dataView?.categorical?.categories?.[3]?.values?.[0];
            const clerkComment = dataView?.categorical?.categories?.[4]?.values?.[0];
            const robotCheck = dataView?.categorical?.categories?.[5]?.values?.[0];
    
            this.id = id;
            this.managerCheckbox.checked = !!managerCheck;
            this.managerComment.value = String(managerComment ?? "");
            this.clerkCheckbox.checked = !!clerkCheck;
            this.clerkComment.value = String(clerkComment ?? "");
            this.robotCheckbox.checked = !!robotCheck;
        }
    }

    /**
     * This function gets called for each of the objects defined in the capabilities files and allows you to select which of the
     * objects and properties you want to expose to the users in the property pane.
     *
     */
    public enumerateObjectInstances(options: EnumerateVisualObjectInstancesOptions): VisualObjectInstanceEnumeration 
    {
        var result = VisualSettings.enumerateObjectInstances(this.settings || VisualSettings.getDefault(), options);

        (Array.isArray(result) ? result : result?.instances).forEach(instance =>
        {
            if (instance?.objectName === "endpoint")
            {
                instance.propertyInstanceKind ??= {}; 
                instance.propertyInstanceKind["url"] = VisualEnumerationInstanceKinds.ConstantOrRule;
            }
        });

        return result;
    }

    private async updateData(e: Event): Promise<void>
    {
        const id = this.id;

        if (!id)
        {
            return;
        }

        const managerCheck = this.managerCheckbox.checked;
        const managerComment = this.managerComment.value;
        const clerkCheck = this.clerkCheckbox.checked;
        const clerkComment = this.clerkComment.value;
        const robotCheck = this.robotCheckbox.checked;

        await fetch(
            this.settings.endpoint.url,
            {
                method: "POST",
                mode: "cors",
                headers: 
                {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(
                { 
                    id, 
                    managerCheck, 
                    managerComment,
                    clerkCheck,
                    clerkComment,
                    robotCheck 
                })
            });

        this.host.refreshHostData();
    }
}

const template = `<form class="status-form">
<fieldset>
    <legend>סטטוס של טיפול</legend>
    <table>
        <tr>
            <td><label for="manager">אישור מנהל</label></td>
            <td><input type="checkbox" name="manager-status" id="manager-status"/></td>
            <td><label for="manager-comment">הערות מנהל</label></td>
            <td><input type="text" name="manager-comment" id="manager-comment"/></td>
        </tr>
        <tr>
            <td><label for="clerk-status">טיפול בנקאי</label></td>
            <td><input type="checkbox" name="clerk-status" id="clerk-status"/></td>
            <td><label for="clerk-comment">הערות בנקאי</label></td>
            <td><input type="text" name="clerk-comment" id="clerk-comment"/></td>
        </tr>
        <tr>
            <td><label for="robot-status">יתרות זכות</label></td>
            <td><input type="checkbox" name="robot-status" id="robot-status"/></td>
            <td colspan="2" class="update-area">
                <button name="update" id="update">אישור</button>
            </td>
        </tr>
    </table>
</fieldset>
</form>`;