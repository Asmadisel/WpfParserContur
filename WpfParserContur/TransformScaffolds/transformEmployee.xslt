<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" indent="yes" encoding="utf-8"/>

	<!-- Группировка по комбинации name + surname -->
	<xsl:key name="employeeKey" match="item" use="concat(@name, '|', @surname)"/>

	<xsl:template match="/Pay">
		<Employees>
			<!-- Ищем ВСЕ элементы <item> на любом уровне вложенности -->
			<xsl:for-each select=".//item[generate-id() = generate-id(key('employeeKey', concat(@name, '|', @surname))[1])]">
				<xsl:variable name="currentName" select="@name"/>
				<xsl:variable name="currentSurname" select="@surname"/>

				<Employee name="{$currentName}" surname="{$currentSurname}">
					<!-- Все записи для этого сотрудника -->
					<xsl:for-each select="key('employeeKey', concat($currentName, '|', $currentSurname))">
						<salary amount="{@amount}" mount="{@mount}"/>
					</xsl:for-each>
				</Employee>
			</xsl:for-each>
		</Employees>
	</xsl:template>

	<!-- Защита от неожиданной структуры XML -->
	<xsl:template match="text()"/>
</xsl:stylesheet>