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
    public class cpt
    {
        //[XmlAttribute]
        public string code { get; set; }
        
        public string authorizationEffectiveDate { get; set; }
        public string authorizationTerminationDate { get; set; }
        public List<string> planNames { get; set; }
    }

    
    public class priorAuthorizationList
    {
        //[XmlAttribute]
        public string payerName { get; set; }
        //[XmlAttribute]
        public string billingPolicyDocumentStartDate { get; set; }
        //[XmlAttribute]
        public string billingPolicyDocumentEndDate { get; set; }
        public List<cpt> cptList { get; set; } = new List<cpt>();
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

        static string ToJson(Object oObject)
        {
            string json = JsonConvert.SerializeObject(oObject);
            return json;
        }

        static string ToStarndardDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return dateStr;

            var date = DateTime.ParseExact(dateStr, "MM/dd/yyyy", null);
            string formatedDate = date.ToString("yyyy/MM/dd");
            return formatedDate;
        }

        static List<string> CSVToArray(string csv, char delimeter)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<string>();

            List<string> items = csv.Split(delimeter).Select(e => e.Trim()).ToList();
            return items;
        }

        static void Main(string[] args)
        {
            
            string payer = "Providence Health Plan";
            string policyStartDt = "2019/04/01";
            string policyEndDt = "2019/06/01";

            //string path = @"C:\Users\Pradeep\Downloads\tabula-PHP_prior_authorization_code_list_short (2).csv";
            string path = @"D:\Cennest\PHP_prior_authorization_blacklist_code_22_April.csv";
            string indexColumn = "Code";
            int headerSpan = 2;
            char delimeter = ',';
            
            var csvSet = new CSVDataSet<Dictionary<string, string>>(path, delimeter, headerSpan : headerSpan);
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

            var providencePriorAuthSet = new priorAuthorizationList();
            providencePriorAuthSet.payerName = payer;
            providencePriorAuthSet.billingPolicyDocumentStartDate = policyStartDt;
            providencePriorAuthSet.billingPolicyDocumentEndDate = policyEndDt;

            foreach (SafeObject<string, string> csvRow in processedRows.Values)
            {
                cpt cpt = new cpt();
                cpt.code = csvRow["Code"];
                cpt.authorizationEffectiveDate = ToStarndardDate(csvRow["Prior AuthorizationEffective Date"]);
                cpt.authorizationTerminationDate = ToStarndardDate(csvRow["Prior AuthorizationTermination Date"]);
                cpt.planNames = CSVToArray(csvRow["Combined PA List"], delimeter);

                providencePriorAuthSet.cptList.Add(cpt);
            }

            string data = ToJson(providencePriorAuthSet);
            string exportfile = Path.ChangeExtension(path, ".json");
            
            using (var writer = new FileWriter(exportfile, false))
            {
                writer.WriteText(data, false);
            }
                
        }

        
    }
}
