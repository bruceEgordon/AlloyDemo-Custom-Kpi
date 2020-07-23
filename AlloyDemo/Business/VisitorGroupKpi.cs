using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using EPiServer.Marketing.KPI.Manager.DataClass;
using EPiServer.Personalization.VisitorGroups;
using EPiServer.Framework.Localization;
using EPiServer;
using System.Text;
using System.Net;
using EPiServer.Core;
using EPiServer.Marketing.KPI.Results;

namespace AlloyDemo.Business
{
    [DataContract]
    public class VisitorGroupKpi : Kpi
    {
        private IVisitorGroupRepository _visitorGroupRepository;
        private IVisitorGroupRoleRepository _visitorGroupRoleRepository;
        private LocalizationService _localizationService;

        [DataMember]
        public string SelectedVisitorGroup { get; set; }

        [DataMember]
        public Guid TestContentGuid { get; set; }

        private EventHandler<ContentEventArgs> _eventHandler;

        public VisitorGroupKpi()
        {
            _visitorGroupRepository = _servicelocator.GetInstance<IVisitorGroupRepository>();
            _visitorGroupRoleRepository = _servicelocator.GetInstance<IVisitorGroupRoleRepository>();
            _localizationService = _servicelocator.GetInstance<LocalizationService>();
        }

        [DataMember]
        public override string UiMarkup
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("<div>");
                sb.Append("Users who match this visitor group will be marked as a conversion goal: ");
                sb.Append("<select name=\"SelectedVisitorGroup\" id=\"SelectedVisitorGroup\">");
                foreach (var visitorGroup in _visitorGroupRepository.List())
                {
                    sb.Append($"<option value=\"{WebUtility.HtmlEncode(visitorGroup.Name)}\">{WebUtility.HtmlEncode(visitorGroup.Name)}</option>");
                }
                sb.Append("</select>");
                sb.Append("</div>");

                return sb.ToString();
            }
        }

        [DataMember]
        public override string UiReadOnlyMarkup
        {
            get
            {
                return $"<div>Selected visitor group: {WebUtility.HtmlEncode(SelectedVisitorGroup)}</div>";
            }
        }

        public override void Validate(Dictionary<string, string> responseData)
        {
            if (responseData["SelectedVisitorGroup"] != null)
            {
                SelectedVisitorGroup = responseData["SelectedVisitorGroup"];
                var contentRef = new ContentReference(responseData["CurrentContent"]);
                var content = _servicelocator.GetInstance<IContentRepository>().Get<IContent>(contentRef);
                TestContentGuid = content.ContentGuid;
                return;
            }
            throw new Exception("Selected visitor group cannot be empty");
        }

        public override IKpiResult Evaluate(object sender, EventArgs e)
        {
            var result = new KpiConversionResult { HasConverted = false, KpiId = Id };
            HttpContext current = HttpContext.Current;
            ContentEventArgs val = e as ContentEventArgs;

            if (current != null && val != null && TestContentGuid == val.Content.ContentGuid)
            {
                //VisitorGroupRole role;
                if (_visitorGroupRoleRepository.TryGetRole(SelectedVisitorGroup, out var role))
                {
                    result.HasConverted = role.IsMatch(current.User, new HttpContextWrapper(current));
                }
            }

            return result;
        }

        public override event EventHandler EvaluateProxyEvent
        {
            add
            {
                _eventHandler = value.Invoke;
                _servicelocator.GetInstance<IContentEvents>().LoadedContent += _eventHandler;
            }
            remove
            {
                _servicelocator.GetInstance<IContentEvents>().LoadedContent -= _eventHandler;
            }
        }

        [DataMember]
        public new string FriendlyName => _localizationService.GetString("/visitorgroupkpi/friendlyname", "Matches a visitor group");

        [DataMember]
        public override string Description => _localizationService.GetString("/visitorgroupkpi/description", "Conversion goal is recorded if a visitor matches the selected visitor group.");
    }
}