using FormatParser.CSVFormater;
using Newtonsoft.Json;
using Playground.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace LumedicExcelParser
{
    public class CPT
    {
        [XmlAttribute]
        public string Code { get; set; }
        
        public string AuthorizationEffectiveDate { get; set; }
        public string AuthorizationTerminationDate { get; set; }
        public string PlanNames { get; set; }
    }

    
    public class PriorAuthorizationList
    {
        [XmlAttribute]
        public string PayerName { get; set; }
        [XmlAttribute]
        public string BillingPolicyDocumentStartDate { get; set; }
        [XmlAttribute]
        public string BillingPolicyDocumentEndDate { get; set; }
        public List<CPT> CPTList { get; set; } = new List<CPT>();
    }

    class Program
    {

        static string ToXML(Object oObject)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlSerializer xmlSerializer = new XmlSerializer(oObject.GetType());
            using (MemoryStream xmlStream = new MemoryStream())
            {
                xmlSerializer.Serialize(xmlStream, oObject);
                xmlStream.Position = 0;
                xmlDoc.Load(xmlStream);
                return xmlDoc.InnerXml;
            }
        }

        static void Main(string[] args)
        {
            

            string payer = "Providence Health Plan";
            string policyStartDt = "4/1/2019";
            string policyEndDt = "6/1/2019";

            //string path = @"C:\Users\Pradeep\Downloads\tabula-PHP_prior_authorization_code_list_short (2).csv";
            string path = @"D:\Cennest\tabula-PHP_prior_authorization_code_list_new.csv";
            string indexColumn = "Code";
            int headerSpan = 2;
            
            var csvSet = new CSVDataSet<Dictionary<string, string>>(path, ',', headerSpan : headerSpan);
            var rows = csvSet.ToList();

            Dictionary<int, SafeObject<string, string>> processedRows = new Dictionary<int, SafeObject<string, string>>();
            int counter = 0;
            foreach (var row in rows)
            {
               
                if (row[indexColumn].ToString().Length > 0)
                    counter++;



                if (processedRows.ContainsKey(counter) == false) processedRows[counter] = SafeObject<string, string>.Create();
                SafeObject<string, string> line = processedRows[counter];

                foreach (var key in row.Keys)
                {
                    string value = row[key];

                    if (value.StartsWith("\"") && value.EndsWith("\""))
                        value = value.Replace("\"", "");

                    
                    line[key] += value.Trim();
                }
            }

            var providencePriorAuthSet = new PriorAuthorizationList();
            providencePriorAuthSet.PayerName = payer;
            providencePriorAuthSet.BillingPolicyDocumentStartDate = policyStartDt;
            providencePriorAuthSet.BillingPolicyDocumentEndDate = policyEndDt;

            foreach (SafeObject<string, string> csvRow in processedRows.Values)
            {
                CPT cpt = new CPT();
                cpt.Code = csvRow["Code"];
                cpt.AuthorizationEffectiveDate = csvRow["Prior AuthorizationEffective Date"];
                cpt.AuthorizationTerminationDate = csvRow["Prior AuthorizationTermination Date"];
                cpt.PlanNames = csvRow["Combined PA List"];

                providencePriorAuthSet.CPTList.Add(cpt);
            }

            string xml = ToXML(providencePriorAuthSet);
            string exportfile = Path.ChangeExtension(path, ".xml");
            
            using (var writer = new FileWriter(exportfile, false))
            {
                writer.WriteText(xml, false);
            }
                
        }
    }
}
