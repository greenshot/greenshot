using System;
using System.Collections.Generic;
using System.Text;

namespace Greenshot.Interop.Remedy {
	public class RemedyExporter {
		private const string HELP_DESK = "HPD:Help Desk";
		private const string INCIDENT_INTERFACE = "HPD:IncidentInterface_Create";
		private const string INCIDENT_MANAGEMENT_CONSOLE = "HPD:Incident Management Console";
		private const int FIELD_INCIDENT = 1000000161;
		private const int FIELD_ATTACH = 536880912;

		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(RemedyExporter));
		public static void Info() {
			try {
				using (IRemedyUserApplication remedyApplication = COMWrapper.GetInstance<IRemedyUserApplication>()) {
					if (remedyApplication != null) {
						//COMWrapper.DumpTypeInfo(remedyApplication);

						ICOMFormWnd2 form = remedyApplication.GetActiveForm();
						//COMWrapper.DumpTypeInfo(form);
						LOG.InfoFormat("Server name {0}", form.GetServerName());
						LOG.InfoFormat("Form name {0}", form.GetFormName());
						if (HELP_DESK.Equals(form.GetFormName())) {
							ICOMField4 field = form.GetFieldById(FIELD_INCIDENT);
							//COMWrapper.DumpTypeInfo(field);
							LOG.InfoFormat("Incident {0}", field.Value);
							ICOMField4 fieldAttach = form.GetFieldById(FIELD_ATTACH);
							LOG.InfoFormat("Attachment {0}", fieldAttach.Value);

						}
					}
				}
			} catch (Exception ex) {
				LOG.Error(ex);
			}
		}
	}
}
