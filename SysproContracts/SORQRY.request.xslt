<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" omit-xml-declaration="yes" indent="yes"/>
  <xsl:template match="/Posting">
    <Query>
      <Key><SalesOrder><xsl:value-of select="SalesOrder"/></SalesOrder><Invoice/></Key>
      <Option>
        <IncludeStockedLines>Y</IncludeStockedLines>
        <IncludeNonStockedLines>Y</IncludeNonStockedLines>
        <IncludeFreightLines>Y</IncludeFreightLines>
        <IncludeMiscLines>Y</IncludeMiscLines>
        <IncludeCommentLines>Y</IncludeCommentLines>
        <IncludeCompletedLines>Y</IncludeCompletedLines>
        <IncludeSerials>Y</IncludeSerials>
        <IncludeLots>Y</IncludeLots>
        <IncludeBins>Y</IncludeBins>
        <IncludeCustomForms>N</IncludeCustomForms>
        <IncludeDetailLineCustomForms>N</IncludeDetailLineCustomForms>
        <IncludeValues>Y</IncludeValues>
        <IncludeKitComponents>Y</IncludeKitComponents>
      </Option>
    </Query>
  </xsl:template>
</xsl:stylesheet>
