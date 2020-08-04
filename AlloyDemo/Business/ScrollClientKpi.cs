using EPiServer.Marketing.KPI.Common.Attributes;
using EPiServer.Marketing.KPI.Exceptions;
using EPiServer.Marketing.KPI.Manager.DataClass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace AlloyDemo.Business
{
    [DataContract]
    public class ScrollClientKpi : ClientKpi
    {
        [DataMember]
        public int ScrollAmount { get; set; }

        [DataMember]
        public override string UiMarkup
        {
            get
            {
                var sb = new StringBuilder();

                sb.Append("<div>");
                sb.Append("<label>Enter scoll amount for conversion</label>");
                sb.Append("<input type=\"text\" name=\"ScrollAmount\" id=\"ScrollAmount\" />");
                sb.Append("</div>");

                return sb.ToString();
            }
        }

        [DataMember]
        public override string UiReadOnlyMarkup => $"<div>Scoll amount set to: {WebUtility.HtmlEncode(ScrollAmount.ToString())}</div>";

        public override void Validate(Dictionary<string, string> responseData)
        {
            if(responseData["ScrollAmount"] == "")
            {
                throw new KpiValidationException("The scroll amount cannot be empty.");
            }
            if(int.TryParse(responseData["ScrollAmount"], out int result) && result > 0 && result < 101)
            {
                ScrollAmount = result;
                return;
            }
            throw new KpiValidationException("The scroll amount has to be a number between 1 and 100.");
        }

        [DataMember]
        public new string FriendlyName => "Detects client browser scrolling";

        [DataMember]
        public override string Description => "Conversion goal is recorded if the visitor scrolls the page the amount defined";

        public override string ClientEvaluationScript 
        {
            get
            {
                string value;
                using(var rdr = new StreamReader(HttpContext.Current.Server.MapPath("~/Resources/ScrollClientEvaluationScript.js")))
                {
                    value = rdr.ReadToEnd();
                    value = Regex.Replace(value, "replace", ScrollAmount.ToString());
                }
                return value;
            }
        }
    }
}