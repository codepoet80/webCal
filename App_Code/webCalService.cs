using System;
using System.Collections;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Net;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Xml.Serialization;
using System.Data;

[WebService(Namespace = "http://software.jonandnic.com/webCal")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class webCalService : System.Web.Services.WebService
{
	public webCalService()
	{

    }

    [WebMethod]
	public string getCalendarData(String calendarURL, String currID, String flushCache) 
	{
		//make file name and id
		if (currID == null || currID == "" || currID.ToLower() == "new") 
			currID = System.Guid.NewGuid().ToString();
		string cacheName = calendarURL.Replace("http://", "");
		cacheName = cacheName.Replace("/", "-");
		String cacheFileName = currID + "_" + cacheName;

		//do cleanup
		cleanupCache("", HttpContext.Current.Server.MapPath("cache/"));

		bool cacheFound = false;
		String[] directoryEntries = System.IO.Directory.GetFileSystemEntries(HttpContext.Current.Server.MapPath("cache/"), "*" + cacheName + "*");
		foreach (string str in directoryEntries)
		{
			FileInfo fi = new FileInfo(str);
			if (fi.Exists && fi.LastWriteTime.AddHours(1) > System.DateTime.Now && flushCache.ToLower() != "true")
			{
				FileInfo cacheFI = new FileInfo(HttpContext.Current.Server.MapPath("cache/" + cacheFileName));
				if (!cacheFI.Exists)
					fi.CopyTo(HttpContext.Current.Server.MapPath("cache/" + cacheFileName));
				cacheFound = true;
			}
			else
			{
				cacheFound = false;
				fi.Delete();
			}
		}
		if (!cacheFound)
		{
			WebRequest feedRequest = WebRequest.Create(calendarURL);
			feedRequest.Timeout = 6000;
			WebResponse feedresponse = feedRequest.GetResponse();
			Stream responseStream = feedresponse.GetResponseStream();
			StreamReader reader = new StreamReader(responseStream);
			StreamWriter sw = File.CreateText(HttpContext.Current.Server.MapPath("cache/" + cacheFileName));
			sw.Write(reader.ReadToEnd());
			sw.Close();
		}
		return currID;
    }

	[WebMethod]
	public string parseCalendarData(string currID)
	{
		DataSet eventsDS = new DataSet();
		eventsDS.DataSetName = "EventData";
		DataTable eventTable = eventsDS.Tables.Add("EventTable");
		eventTable.TableName = "Event";
		String[] directoryEntries = System.IO.Directory.GetFileSystemEntries(HttpContext.Current.Server.MapPath("cache/"), currID + "_*");
		if (directoryEntries.Length > 0)
		{
			DataColumn uidCol = new DataColumn();
			uidCol.DataType = System.Type.GetType("System.String");
			uidCol.ColumnName = "UID";
			eventTable.Columns.Add(uidCol);

			DataColumn stCol = new DataColumn();
			stCol.DataType = System.Type.GetType("System.Decimal");
			stCol.ColumnName = "STARTTICKS";
			eventTable.Columns.Add(stCol);

			DataColumn tCol = new DataColumn();
			tCol.DataType = System.Type.GetType("System.String");
			tCol.ColumnName = "STARTTIME";
			eventTable.Columns.Add(tCol);

			DataColumn etCol = new DataColumn();
			etCol.DataType = System.Type.GetType("System.String");
			etCol.ColumnName = "ENDTIME";
			eventTable.Columns.Add(etCol);

			DataColumn sCol = new DataColumn();
			sCol.DataType = System.Type.GetType("System.String");
			sCol.ColumnName = "SUMMARY";
			eventTable.Columns.Add(sCol);

			DataColumn dCol = new DataColumn();
			dCol.DataType = System.Type.GetType("System.String");
			dCol.ColumnName = "DURATION";
			eventTable.Columns.Add(dCol);

			DataColumn nCol = new DataColumn();
			nCol.DataType = System.Type.GetType("System.String");
			nCol.ColumnName = "CALENDARNAME";
			eventTable.Columns.Add(nCol);

			foreach (string str in directoryEntries)
			{
				String iCalData;
				StreamReader sr = File.OpenText(str);
				iCalData = sr.ReadToEnd();
				sr.Close();

				string[] calendarNameParts = str.Split('-');
				string calendarName = calendarNameParts[calendarNameParts.Length-1];
				calendarNameParts = calendarName.Split('.');
				calendarName = calendarNameParts[0];

				String delimStr = "BEGIN:VEVENT";
				string[] events = System.Text.RegularExpressions.Regex.Split(iCalData, delimStr, System.Text.RegularExpressions.RegexOptions.None);
				string[] eventCols = events[1].Split('\n');
				for (int i = 1; i < events.Length; i++)
				{
					if (events[i].IndexOf("MICROSOFT") == -1 && events[i].IndexOf("REQ-PARTICIPANT") == -1)
					{
						events[i] = events[i].Replace("&", "&amp;");
						events[i] = events[i].Replace("\\", "-");
						events[i] = events[i].Replace("/", "-");
						events[i] = events[i].Replace("MAILTO:", "");

						string[] eventParts = events[i].Split('\n');
						DataRow eventRow = eventTable.NewRow();

						Hashtable attribs = new Hashtable();
						for (int e = 0; e < eventParts.Length; e++)
						{
							if (eventParts[e].IndexOf(":") != -1)
							{
								String keyName = eventParts[e].Substring(0, eventParts[e].IndexOf(":"));
								if (keyName.IndexOf(";") != -1)
									keyName = keyName.Substring(0, keyName.IndexOf(";"));
								String keyValue = eventParts[e].Substring(eventParts[e].IndexOf(":") + 1, eventParts[e].Length - (eventParts[e].IndexOf(":") + 1));
								if (keyName.Length > 1 && !attribs.ContainsKey(keyName))
									attribs.Add(keyName, keyValue);
							}
						}

						eventRow["UID"] = (string)attribs["UID"];
						if (makeDateTime((string)attribs["DTSTART"]) != "")
						{
							eventRow["STARTTIME"] = DateTime.Parse(makeDateTime((string)attribs["DTSTART"]));
							eventRow["STARTTICKS"] = DateTime.Parse(makeDateTime((string)attribs["DTSTART"])).Ticks;
						}
						if (makeDateTime((string)attribs["DTEND"]) != "")
							eventRow["ENDTIME"] = DateTime.Parse(makeDateTime((string)attribs["DTEND"]));
						eventRow["SUMMARY"] = (string)attribs["SUMMARY"];
						eventRow["DURATION"] = (string)attribs["DURATION"];
						eventRow["CALENDARNAME"] = calendarName;
						eventTable.Rows.Add(eventRow);

						if ((string)attribs["RRULE"] != null)
						{
							DateTime recurStart = DateTime.Parse(makeDateTime((string)attribs["DTSTART"]));
							DateTime recurEnd = new DateTime();
							if (((string)attribs["DTEND"]) != null)
								recurEnd = DateTime.Parse(makeDateTime((string)attribs["DTEND"]));
							string rrules = (string)attribs["RRULE"];
							string[] rrule = rrules.Split(';');
							string freq, interval, bymonthday, byday, until;
							int count = 0;
							interval = "";
							freq = "";
							bymonthday = "";
							byday = "";
							until = "";
							for (int r = 0; r < rrule.Length; r++)
							{
								string[] rParts = rrule[r].Split('=');
								string currPart = rParts[0];
								if (currPart.ToLower() == "freq")
									freq = rParts[1];
								if (currPart.ToLower() == "interval")
									interval = rParts[1];
								if (currPart.ToLower() == "count")
									count = Convert.ToInt16(rParts[1]);
								if (currPart.ToLower() == "bymonthday")
									bymonthday = rParts[1];
								if (currPart.ToLower() == "byday")
									bymonthday = rParts[1];
								if (currPart.ToLower() == "until")
									until = rParts[1];
							}
							if (count == 0)
								count = 100;
							if (until != "")
							{
								DateTime lastDay = DateTime.Parse(makeDateTime(until));
								System.TimeSpan diff1 = recurStart.Subtract(lastDay);
								count = diff1.Days;
								if (freq.ToLower() == "weekly")
									count = (count / 7);
								if (freq.ToLower() == "monthly")
									count = (count / 31);
								if (freq.ToLower() == "yearly")
									count = (count / 365);

							}
							for (int ri = 0; ri < count; ri++)
							{
								if (freq.ToLower() == "daily")
								{
									int addDays = Convert.ToInt16(interval) * 1;
									recurStart = recurStart.AddDays(addDays);
									if (((string)attribs["DTEND"]) != null)
										recurEnd = recurEnd.AddDays(addDays);
								}
								if (freq.ToLower() == "weekly")
								{
									int addDays = Convert.ToInt16(interval) * 7;
									recurStart = recurStart.AddDays(addDays);
									if (((string)attribs["DTEND"]) != null)
										recurEnd = recurEnd.AddDays(addDays);
								}
								if (freq.ToLower() == "monthly")
								{
									int addMonths = Convert.ToInt16(interval) * 1;
									recurStart = recurStart.AddMonths(addMonths);
									string dateString = recurStart.Month + "/" + Convert.ToInt16(bymonthday) + "/" + recurStart.Year;
									recurStart = DateTime.Parse(dateString);
									if (((string)attribs["DTEND"]) != null)
										recurEnd = recurEnd.AddMonths(addMonths);
								}
								if (freq.ToLower() == "yearly")
								{
									int addYears = Convert.ToInt16(interval) * 1;
									recurStart = recurStart.AddYears(addYears);
									if (((string)attribs["DTEND"]) != null)
										recurEnd = recurEnd.AddYears(addYears);
								}

								DataRow eventRowR = eventTable.NewRow();
								eventRowR["UID"] = (string)attribs["UID"];
								eventRowR["STARTTIME"] = recurStart;
								eventRowR["STARTTICKS"] = recurStart.Ticks;
								if (((string)attribs["DTEND"]) != null)
									eventRowR["ENDTIME"] = recurEnd;
								eventRowR["SUMMARY"] = (string)attribs["SUMMARY"];
								eventRowR["DURATION"] = (string)attribs["DURATION"];
								eventRowR["CALENDARNAME"] = calendarName;
								eventTable.Rows.Add(eventRowR);
							}
						}
					}
				}
			}

			//Sort Data
			XmlDocument xmlCache = new XmlDocument();
			xmlCache.LoadXml(eventsDS.GetXml());
			xmlCache.Save(HttpContext.Current.Server.MapPath("cache/" + currID + ".xml"));
			return currID;
		}
		else
		{
			return "expired";
		}
	}

	[WebMethod]
	public string getEventsInRange(string currID, string startDate, string endDate)
	{
		if (startDate == null || startDate == "")
			startDate = System.DateTime.Now.ToShortDateString();
		if (endDate == null || endDate == "")
			endDate = System.DateTime.Now.AddDays(31).ToShortDateString();

		DataSet eventsDS = new DataSet();
		XmlDocument xmlCache = new XmlDocument();
		FileInfo fi = new FileInfo(HttpContext.Current.Server.MapPath("cache/" + currID + ".xml"));
		if (fi.Exists)
		{
			xmlCache.Load(HttpContext.Current.Server.MapPath("cache/" + currID + ".xml"));
			System.IO.StringReader xmlSR = new System.IO.StringReader(xmlCache.OuterXml);
			eventsDS.ReadXml(xmlSR);

			DataView dvSort = new DataView(eventsDS.Tables[0]);
			dvSort.Sort = "STARTTICKS ASC";

			float startDateTime = 0;
			float endDateTime = 0;
			string filter = "";
			try
			{
				startDateTime = DateTime.Parse(startDate).Ticks;
			}
			catch (Exception ex)
			{
				startDate = "";
			}
			if (startDate != "")
				filter = "STARTTICKS > " + startDateTime + " AND ";

			try
			{
				endDateTime = DateTime.Parse(endDate).Ticks;
			}
			catch (Exception ex)
			{
				endDate = "";
			}
			if (endDate != "")
				filter += "STARTTICKS < " + endDateTime + "";
			dvSort.RowFilter = filter;

			DataTable dtSorted = dvSort.Table.Clone();
			foreach (DataRowView drv in dvSort)
			{
				dtSorted.ImportRow(drv.Row);
			}
			DataSet sortedDS = new DataSet();
			sortedDS.DataSetName = "Events";
			sortedDS.Tables.Add(dtSorted);
			return sortedDS.GetXml();
		}
		else
		{
			return "expired";
		}
	}

	public string makeDateTime(string dateTime)
	{
		String currDate = "";
		String useDate;
		if (dateTime != null && dateTime != "")
			useDate = dateTime;
		else
			useDate = "";
		if (useDate.Length >= 8)
		{
			currDate += useDate.Substring(4, 2) + "/";
			currDate += useDate.Substring(6, 2) + "/";
			currDate += useDate.Substring(0, 4) + " ";
		}
		if (useDate.Length >= 15)
		{
			currDate += useDate.Substring(9, 2) + ":";
			currDate += useDate.Substring(11, 2) + ":";
			currDate += useDate.Substring(13, 2);
		}
		return currDate;
	}

	[WebMethod]
	public string cleanupCache(string currID, string cachePath)
	{
		if (cachePath == null || cachePath == "")
			cachePath = HttpContext.Current.Server.MapPath("cache/");
		if (currID != "")
			currID = "*" + currID + "*";
		else
			currID = "*";
		String[] directoryEntries = System.IO.Directory.GetFileSystemEntries(cachePath, currID);
		foreach (string str in directoryEntries)
		{
			FileInfo fi = new FileInfo(str);
			if (fi.LastWriteTime.AddHours(1) < System.DateTime.Now || currID != "*")
			{
				try
				{
					fi.Delete();
				}
				catch (Exception ex)
				{
					//who cares
				}
			}
		}
		return "cache cleared";
	}
}

