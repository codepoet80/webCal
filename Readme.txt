webCal
By: Jonathan Wise
www.jonandnic.com

Requirements
webCal requires jsObjects 2.5.1 or better. Please download the latest jsObjects
package at http://software.jonandnic.com/jsObjects

License
webCal is distributed under a Creative Commons Licenses for non-commercial use.
If you would like to use this application for commercial purposes, please contact the author.
webCal can be modified or used in derivative works provided the author is credited and this
license remains intact.

How to Use
Modify the webCalConfig.xml to provide information for each calendar to be displayed.
Alternatively, pass calendar URLS, pipe-seperated (|) in the query parameter "calendar"
Ie: http://server/webCal?calendar=http://server/mycal.ics

Change Log
0.5		- Loads vCal files from remote source, minus recurring events
		  Displays events from all sources in one calendar
0.6		- Seperated out multiple calendars using colors and checkboxes
0.7		- Added support for all-day and recurring events
1.0		- Initial public release
1.1		- Found out about "until" parameter of recurring events, adjusted accordingly