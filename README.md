# crmTools

A collection of XrmToolBox plugins built for Dynamics 365 / Power Platform productivity tasks.
---
## Tools
| Tool | Description |
| [Email Template Mover](#email-template-mover) | Transfer email templates across Dynamics 365 environments |

---
## Email Template Mover
### What it does
Transfers email templates from one Dynamics 365 environment to another directly inside XrmToolBox. It handles the Entity Type Code (ETC) remapping behind the scenes — so entity references inside the template HTML stay intact after the move.
> **Important:** This tool is intended for transferring templates between **unmanaged environments only**.
> Do not use it to push templates into a managed environment or an environment that has a managed solution layer — doing so may create an unmanaged customization layer on top of the managed solution, which can cause issues during future solution upgrades.

---
### When to use it
- You have an email template in a dev or UAT environment and want to move it to another unmanaged org.
- You want to bulk-migrate templates without manually re-creating them.
- The template references a specific entity (e.g. Contact, Account) and you need the ETC to be correct in the target environment.

---
### How it works
The plugin uses **XrmToolBox's multi-organization connection** feature — you connect to a source environment through XrmToolBox as usual, then add a second connection for the target environment using the **Select Target** button.
For each template transferred:
1. It reads the entity the template is linked to from the source.
2. It looks up the matching entity in the target environment (since ETCs can differ between orgs).
3. It rewrites any ETC references embedded in the template's HTML and XML content.
4. It checks if a template with the same name already exists in the target — if yes it updates it, if no it creates a new one.

---
### How to use it
1. Open XrmToolBox and connect to your **source** environment.
2. Open **Email Template Mover** from the tool list.
3. Click **Load Templates** to pull in all templates from the source.
4. Use the **search box** to filter by name if needed.
5. Click **Select Target** to add your target environment connection.
6. Select the templates you want to transfer.
7. Click **Transfer** and watch the log panel for results.

---
### Debugging
To debug the plugin locally:
1. Clone this repo and open the solution in Visual Studio.
2. Build in **Debug** mode — the post-build step copies the DLL into a `Plugins\` subfolder automatically.
3. Copy that `Plugins\` folder output into your XrmToolBox `Plugins` directory.
4. In Visual Studio, go to **Debug → Attach to Process** and attach to the `XrmToolBox.exe` process.
5. Set breakpoints in the code as needed and reproduce the issue inside XrmToolBox.
Key files to look at when debugging:

| File | What's in it |
|---|---|
| `PluginControl.cs` | UI logic — button clicks, loading templates, triggering the transfer |
| `TemplatesMover.cs` | Core logic — CRM queries, ETC resolution, upsert operations |
| `Plugin.cs` | Plugin registration metadata for XrmToolBox |

---
### Tech used
- **XrmToolBox SDK** — `MultipleConnectionsPluginControlBase` for multi-org support
- **Microsoft CRM SDK** — `IOrganizationService`, `QueryExpression`, `RetrieveEntityRequest`
- **WinForms** — UI, `BackgroundWorker` for async transfer
- **C# / .NET Framework 4.8**
