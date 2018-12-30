# NginxLogSharp
Nginx日志工具

# 配置
请自行新增 App.config 文件并添加节点
<connectionStrings>
    <add name="SqlConnString" connectionString="server=.;database=Data;uid=sa;pwd=sa;Connection Timeout=30;Application Name=nginxlog" providerName="System.Data.SqlClient" />
</connectionStrings>

项目内有 SQL脚本，在sql server中加入并执行即可

# 常用SQL

查找访问次数最多的地址页面
SELECT iCount=COUNT(*),cLogUrl,id=MAX(id)
FROM NginxLog 
GROUP BY cLogUrl
ORDER BY COUNT(*) DESC

查找访问最占用流量的地址记录
SELECT TOP 100 iLogLength1=iLogLength/1024,* 
FROM NginxLog 
ORDER BY iLogLength DESC