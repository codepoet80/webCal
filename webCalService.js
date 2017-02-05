function loadCalendar(calendarPath, calendarName, currToken)
{
	myCal.setStatus("working", "Requesting remote calendar: " + calendarName);	    
	myWS.getCalendarData(calendarPath, currToken, "", calendarLoaded);
}

calendarPos = 0;
function calendarLoaded(responseText)
{
	var currToken;
	if (responseText != "" && responseText != null && responseText != undefined)
	    currToken = findResponseNode (responseText, "getCalendarDataResult");
	else
	    currToken = ""
	if (calendarPos < appCalList.length)
	{
	    loadCalendar(appCalList[calendarPos].path, appCalList[calendarPos].name, currToken);
	    calendarPos ++;
	}
	else
	    parseCalendars(currToken);
}

function parseCalendars(currToken)
{
    sessionToken = currToken;
	myCal.setStatus("working", "Aggregating event data...");
    myWS.parseCalendarData(currToken, callForEvents);
}

loadedMonths = new Array();
function callForEvents(responseText, currToken)
{
    var stDate, endDate;
    stDate = (myCal.calendarDate.getMonth()+1) + "/1/" + (myCal.calendarDate.getFullYear());
    endDate = (myCal.calendarDate.getMonth()+1) + "/" + CalendarPvtGetMonthLength(CalendarPvtGetMonthName(myCal.calendarDate.getMonth()), myCal.calendarDate) + "/" + (myCal.calendarDate.getFullYear());
    var found = false;
    for (var a=0;a<loadedMonths.length;a++)
    {
        if (loadedMonths[a] == stDate)
            found = true;
    }
    if (!found)
    {
        myCal.setStatus("working", "Populating calendar...");
        loadedMonths[loadedMonths.length] = stDate;
	    if (currToken == null)
	        currToken = findResponseNode (responseText, "parseCalendarDataResult");
	    var response = myWS.getEventsInRange(currToken, stDate, endDate, populateEvents);
    }
}

var appEventList = new Array();
function populateEvents(responseText)
{
    appEventList = new Array();
	var eventData = findResponseNode (responseText, "getEventsInRangeResult");
	var eventsArray = new Array();
	if (browser == "IE")
	{
		var xmlDoc = new ActiveXObject("MSXML2.DOMDocument");
		xmlDoc.async = false;
		if (xmlDoc.loadXML(xmlDecode(responseText)))
		{
			var nodes = xmlDoc.selectNodes("//Event");
			for (var n=0;n<nodes.length;n++)
			{
				var percentComplete = n / nodes.length;
				percentComplete = Math.round(percentComplete);
				var xmlEvent = new ActiveXObject("MSXML2.DOMDocument");
				xmlEvent.loadXML(nodes[n].xml)
				var eventObj = new Object();
				eventObj.uid = xmlEvent.selectSingleNode("//UID").text + "";
				eventObj.uid += "_" + xmlEvent.selectSingleNode("//STARTTICKS").text + "";
				eventObj.summary = xmlEvent.selectSingleNode("//SUMMARY").text + "";
				eventObj.calendar = xmlEvent.selectSingleNode("//CALENDARNAME").text + "";

				var startTime = xmlEvent.selectSingleNode("//STARTTIME").text + "";
				eventObj.startDate = makeDateObj(startTime);
				
				try
				{
				    var endTime = xmlEvent.selectSingleNode("//ENDTIME").text + "";
				    eventObj.endDate = makeDateObj(endTime);
				}
				catch(e)
				{
				    eventObj.endDate = "none";
				}
				
				appEventList[appEventList.length] = eventObj;
			}
		}
		else
			alert ("error");
	}
	else if (browser == "MOZ")
	{
		var xmlParser = new DOMParser();
		var xmlDoc = xmlParser.parseFromString(xmlDecode(responseText), 'text/xml');
		var nodes = xmlDoc.getElementsByTagName("Event");
		for (var n=0;n<nodes.length;n++)
		{
			var eventObj = new Object();
			var xmlEvent = nodes[n].getElementsByTagName("UID");
			eventObj.uid = xmlEvent[0].firstChild.nodeValue + "";
			var xmlEvent = nodes[n].getElementsByTagName("STARTTICKS");
			eventObj.uid += + "_" +xmlEvent[0].firstChild.nodeValue + "";
			var xmlEvent = nodes[n].getElementsByTagName("SUMMARY");
			eventObj.summary = xmlEvent[0].firstChild.nodeValue + "";
			var xmlEvent = nodes[n].getElementsByTagName("CALENDARNAME");
			eventObj.calendar = xmlEvent[0].firstChild.nodeValue + "";
			
			var xmlEvent = nodes[n].getElementsByTagName("STARTTIME");
			var startTime = xmlEvent[0].firstChild.nodeValue + "";
			eventObj.startDate = makeDateObj(startTime);
			
		    var xmlEvent = nodes[n].getElementsByTagName("ENDTIME");
		    if (xmlEvent.length != 0)
		    {
		        var endTime = xmlEvent[0].firstChild.nodeValue + "";
		        eventObj.endDate = makeDateObj(endTime);
		        
		    }
		    else
		        eventObj.endDate = "none";
			appEventList[appEventList.length] = eventObj;
		}
	}
    drawAppEvents();
}

function makeDateObj(startTime)
{
    var eventDate = new Date();
    var mstartDate = (startTime + "").split(" ");
	var mstartTime = mstartDate[1];
	var mstartAP = mstartDate[2];

	mstartDate = mstartDate[0];
	var startDateParts = mstartDate.split("/");
	eventDate.setDate(startDateParts[1]);
	eventDate.setMonth(startDateParts[0]-1);
	eventDate.setFullYear(startDateParts[2]);
	
	var startTimeParts = mstartTime.split(":");
	startTimeParts[0] = startTimeParts[0] * 1;
	if (mstartAP.toLowerCase() == "am")
	    startTimeParts[0] = startTimeParts[0] - 1;
	else
	    startTimeParts[0] = startTimeParts[0] + 11;
	eventDate.setHours(startTimeParts[0]);
	eventDate.setMinutes(startTimeParts[1]);
	eventDate.setSeconds(startTimeParts[2]);
	return eventDate;
}

function findResponseNode(responseText, findNode)
{
	var response = "";
	if (browser == "IE" && findNode != "")
	{
		var xmlDoc = new ActiveXObject("MSXML2.DOMDocument");
		xmlDoc.async = false;
		if (xmlDoc.loadXML(responseText))
		{
			findNode = "//" + findNode;
			response = xmlDoc.selectSingleNode(findNode);
			if (response != null && response.childNodes.length > 0)
				response = response.firstChild.xml;
			else
				response = "";
		}
		else
			response = "";
	}
	else if (browser == "MOZ" && findNode != "")
	{
		xmlParser = new DOMParser();
		xmlDoc = xmlParser.parseFromString(responseText, 'text/xml');
		var nodes = xmlDoc.getElementsByTagName(findNode);
		if (nodes.length > 0)
		{
			if (nodes[0].childNodes.length > 0)
				response = nodes[0].firstChild.nodeValue;
			else
				response = "";
		}
		else
			response = "";
	}
	return response;
}