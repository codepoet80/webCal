<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:webCal="http://software.jonandnic.com/webCal">

<xsl:template match="//Events">
	<channel>
		<xsl:for-each select="Event">
			<xsl:sort select="DATEVAL" order="descending"/>
			<item>
				<title>
					<xsl:value-of select="SUMMARY"/>
				</title>
				<pubDate>
					<xsl:value-of select="DTSTART"/>
				</pubDate>
				<webCal:endDate>
					<xsl:value-of select="DTEND"/>
				</webCal:endDate>
				<webCal:duration>
					<xsl:value-of select="DURATION"/>
				</webCal:duration>
				<guid>
					<xsl:value-of select="UID"/>
				</guid>
				<description>
					<xsl:value-of select="DESCRIPTION"/>
				</description>

			</item>
		</xsl:for-each>
	</channel>
</xsl:template>

</xsl:stylesheet> 

