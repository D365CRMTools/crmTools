using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailTemplateMover
{
    internal class TemplatesMover
    {
        public List<Entity> GetTemplates(IOrganizationService service)
        {
            QueryExpression qe = new QueryExpression("template") { ColumnSet = new ColumnSet("title", "createdon", "modifiedon", "templatetypecode", "languagecode") };
            var results = service.RetrieveMultiple(qe);
            if (results != null && results.Entities != null && results.Entities.Count > 0)
            {
                return results.Entities.ToList();
            }
            return new List<Entity>();
        }
        public string GetEntityTypeCode(IOrganizationService service, string entityName)
        {
            RetrieveEntityRequest request = new RetrieveEntityRequest();
            request.LogicalName = entityName;
            request.EntityFilters = EntityFilters.Entity;
            RetrieveEntityResponse response = (RetrieveEntityResponse)service.Execute(request);
            EntityMetadata metadata = response.EntityMetadata;
            return metadata.LogicalName;
        }
        public string GetEntityLogicalNameFromCode(IOrganizationService service, string objectCode)
        {
            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = true
            };
            var response = (RetrieveAllEntitiesResponse)service.Execute(request);
            var metadata = response.EntityMetadata.FirstOrDefault(e => e.LogicalName == objectCode);
            return metadata != null ? metadata.LogicalName : string.Empty;
        }
        public Guid TemplateExists(IOrganizationService service, string name)
        {
            Guid result = Guid.Empty;
            QueryExpression qe = new QueryExpression("template");
            qe.Criteria.AddCondition("title", ConditionOperator.Equal, name);
            var results = service.RetrieveMultiple(qe);
            if (results != null && results.Entities != null && results.Entities.Count > 0)
            {
                result = results[0].Id;
            }
            return result;
        }
        public void ReRouteEtcViaOpenXML(Entity template, string name, string etc, string oldEtc, string newEtc)
        {
            if (oldEtc == null || newEtc == null || oldEtc == newEtc)
                return;
            string[] contentFields = { "body", "subject", "presentationxml", "languagecode" };
            foreach (string fieldName in contentFields)
            {
                if (template.Contains(fieldName) && template[fieldName] != null)
                {
                    string content = template[fieldName].ToString();
                    if (!string.IsNullOrEmpty(content))
                    {
                        string toFind = string.Format("{0}/{1}", etc, oldEtc);
                        string replaceWith = string.Format("{0}/{1}", etc, newEtc);
                        string updatedContent = content.Replace(toFind, replaceWith);
                        string toFindEncoded = Uri.EscapeDataString(toFind);
                        string replaceWithEncoded = Uri.EscapeDataString(replaceWith);
                        updatedContent = updatedContent.Replace(toFindEncoded, replaceWithEncoded);
                        template[fieldName] = updatedContent;
                    }
                }
            }
        }


    }
}
