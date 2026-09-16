using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Args;

namespace EmailTemplateMover
{
    public partial class PluginControl : MultipleConnectionsPluginControlBase
    {
        private Settings mySettings;
        private readonly TemplatesMover myTemplatesMover;
        public event EventHandler<StatusBarMessageEventArgs> SendMessageToStatusBar;

        public PluginControl()
        {
            InitializeComponent();
            myTemplatesMover = new TemplatesMover();
        }
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName = "", object parameter = null)
        {
            ConnectionDetail = detail;
            if (actionName == "AdditionalOrganization")
            {
                AdditionalConnectionDetails.Clear();
                AdditionalConnectionDetails.Add(detail);
                SetConnectionLabel(detail, "Target");
            }
            else
            {
                SetConnectionLabel(detail, "Source");
            }
            base.UpdateConnection(newService, detail, actionName, parameter);
        }
        private void PluginControl_Load(object sender, EventArgs e)
        {
            ShowInfoNotification("This is a notification that can lead to XrmToolBox repository", new Uri("https://github.com/MscrmTools/XrmToolBox"));
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();
                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }
        }
        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }
        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PluginControl_OnCloseTool(object sender, EventArgs e)
        {
            SettingsManager.Instance.Save(GetType(), mySettings);
        }
        protected override void ConnectionDetailsUpdated(NotifyCollectionChangedEventArgs e)
        {
        }
        private void transTemp_Click(object sender, EventArgs e)
        {
            if (listView1.CheckedItems.Count > 0 || listView1.SelectedItems.Count>0) {
                var selectedTemplates = listView1.CheckedItems.Cast<ListViewItem>()
                    .Union(listView1.SelectedItems.Cast<ListViewItem>()).Select(item => (Entity)item.Tag).ToList();
                ldtemp.Enabled = false;
                trsTemp.Enabled = false;
                btnSelectTarg.Enabled = false;
                var transfer = new BackgroundWorker();
                transfer.DoWork += (s, evt) =>
                {
                    List<Entity> templates = (List<Entity>)evt.Argument;
                    var total = templates.Count;
                    var current = 0;
                    var targetService = AdditionalConnectionDetails.First().GetCrmServiceClient();
                    foreach (var template in templates) {
                        current++;
                        string name = template.GetAttributeValue<string>("title");
                        string sourceTypeCodeStr = template.GetAttributeValue<string>("templatetypecode");
                        string oldEtc = sourceTypeCodeStr;
                        string newEtc ;
                        string targetedEntityLogicalName = "";
                        try
                        {
                            targetedEntityLogicalName = myTemplatesMover.GetEntityLogicalNameFromCode(Service, sourceTypeCodeStr);
                            newEtc = myTemplatesMover.GetEntityTypeCode(targetService, targetedEntityLogicalName);                         
                            myTemplatesMover.ReRouteEtcViaOpenXML(template, name,"template", oldEtc, newEtc);
                            var templateToTransfer = new Entity(template.LogicalName);
                            string[] attributesToSkip = { "templateid", "createdon", "modifiedon", "versionnumber" };
                            foreach (var attribute in template.Attributes)
                            {
                                if (attributesToSkip.Contains(attribute.Key.ToLower())) continue;
                                templateToTransfer[attribute.Key] = attribute.Value;
                            }
                            if (sourceTypeCodeStr != null)
                            {
                                templateToTransfer["templatetypecode"] = sourceTypeCodeStr;
                            }
                            else
                            {
                                templateToTransfer["templatetypecode"] = newEtc;
                            }

                            Guid existingId = myTemplatesMover.TemplateExists(targetService, name);
                            if (existingId != null && existingId != Guid.Empty)
                            {
                                templateToTransfer["templateid"] = existingId;
                                targetService.Update(templateToTransfer);
                            }
                            else
                            {
                                targetService.Create(templateToTransfer);
                            }
                            Log(name, true);

                        }
                        catch (Exception error)
                        {
                            Log(name, false, error.Message);
                        }
                    }
                };
                transfer.ProgressChanged += (s, evt) =>
                {
                };
                transfer.RunWorkerCompleted += (s, evt) =>
                {
                    if (evt.Error != null)
                    {
                        MessageBox.Show(ParentForm, @"An error has occured while transferring templates: " + evt.Error.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    ldtemp.Enabled = true;
                    trsTemp.Enabled = true;
                    btnSelectTarg.Enabled = true;
                };
                transfer.WorkerReportsProgress = true;
                transfer.RunWorkerAsync(selectedTemplates);
            }
            else
            {
                MessageBox.Show(@"You have to select at least one source template and a target organization to continue.", @"Warning",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void btnSelectTarg_Click(object sender, EventArgs e)
        {
            AddAdditionalOrganization();
        }
        private void SetConnectionLabel(ConnectionDetail detail, string serviceType)
        {
            switch (serviceType)
            {
                case "Source":
                    lblSource_Env_nm.Text = detail.ConnectionName;
                    lblSource_Env_nm.ForeColor = Color.Green;
                    break;
                case "Target":
                    lblTarget_Env_nm.Text = detail.ConnectionName;
                    lblTarget_Env_nm.ForeColor = Color.Green;
                    break;
            }
        }
        private void ldtemp_Click(object sender, EventArgs e)
        {
            ExecuteMethod(RetrieTemp);
        }
        private void RetrieTemp()
        {
            listView1.Items.Clear();
            ldtemp.Enabled = false;
            trsTemp.Enabled = false;
            btnSelectTarg.Enabled = false;
            var back_work = new BackgroundWorker();
            back_work.DoWork += (sender, e) =>
            {
                e.Result = myTemplatesMover.GetTemplates(Service);
            };
            back_work.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error == null)
                {
                    foreach (var template in (List<Entity>)e.Result)
                    {
                        var item = new ListViewItem();
                        item.Tag = template;
                        item.Text = template.GetAttributeValue<string>("title");
                        var createdOn = template.GetAttributeValue<DateTime>("createdon");
                        item.SubItems.Add(createdOn.ToString());
                        var modifiedOn = template.GetAttributeValue<DateTime>("modifiedon").ToString();
                        item.SubItems.Add(modifiedOn.ToString());
                        var category = template.GetAttributeValue<string>("templatetypecode").ToString();
                        item.SubItems.Add(category);
                        listView1.Items.Add(item);
                    }
                }
                else
                {
                    MessageBox.Show(ParentForm, @"An error has occured while loading templates: " + e.Error.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                ldtemp.Enabled = true;
                trsTemp.Enabled = true;
                btnSelectTarg.Enabled = true;
            };
            back_work.RunWorkerAsync();
        }
        private void Log(string name, bool succeeded, string message = null)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    listBox_log.Items.Add(
                        $"{(succeeded ? "Success" : "Error")}: {name}{(message != null ? " : " + message : "")}");
                });
            }
        }
    }
}