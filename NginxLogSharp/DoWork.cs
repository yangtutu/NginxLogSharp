using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DapperExtensions;
using DapperExtensions.Sql;

namespace NginxLogSharp
{
    class DoWork
    {

        public DoWork()
        {
            string connString = GetSqlConnString();
            if (string.IsNullOrWhiteSpace(connString)) return;

            using (IDbConnection connection = new SqlConnection(connString))
            {
                if (connection.State == ConnectionState.Closed) connection.Open();

                //当前目录下
//                string path = @"C:\Users\Administrator\Desktop\Debug\nginx-default.access.log";
                string path = System.IO.Directory.GetCurrentDirectory() + "\\nginx-default.access.log";
                Console.WriteLine(path);
                if (!File.Exists(path))
                {
                    Console.WriteLine("no file");
                    Console.ReadKey();
                    return;
                }

                List<NginxLog> lsNginxLog = new List<NginxLog>();
                StreamReader sr = new StreamReader(path, Encoding.Default);  //path为文件路径
                String line;
                int iCount = 0;
                int iPageSize = 1000000;
                int iPageIndex =0;
                while ((line = sr.ReadLine()) != null)//按行读取 line为每行的数据
                {
                    iCount++;
                    
                    //  Console.WriteLine(iCount + ":" + line);
                    string[] lineConArr = line.Split('"');
                    if (lineConArr.Length > 0)
                    {
                        string cLogIp = string.Empty;
                        string cLogDate = string.Empty;
                        string cLogTime = string.Empty;
                        string cLogMethod = string.Empty;
                        string cLogFile = string.Empty;
                        string cLogProtocol = string.Empty;
                        string cLogStatus = string.Empty;
                        int iLogLength = 0;
                        string cLogUrl = string.Empty;
                        string cLogUserAgent = string.Empty;

                        //183.214.46.22 - - [11/Dec/2018:09:38:08 +0800] 
                        string ipAndTime = lineConArr[0];
                        if (!string.IsNullOrWhiteSpace(ipAndTime))
                        {
                            cLogIp = ipAndTime.Substring(0, ipAndTime.IndexOf("-", StringComparison.Ordinal));
                            if (cLogIp.Length > 50) cLogIp = cLogIp.Substring(0, 50);
                            string dateTime = ipAndTime.Substring(ipAndTime.IndexOf("[", StringComparison.Ordinal) + 1, ipAndTime.Length - ipAndTime.IndexOf(":", StringComparison.Ordinal) + 3);
                            cLogDate = Convert.ToDateTime(dateTime.Substring(0, dateTime.IndexOf(":", StringComparison.Ordinal))).ToString("yyyy-MM-d");
                            cLogTime = dateTime.Substring(dateTime.IndexOf(":", StringComparison.Ordinal) + 1, 8);
                            if (cLogDate.Length > 50) cLogDate = cLogDate.Substring(0, 50);
                            if (cLogTime.Length > 50) cLogTime = cLogTime.Substring(0, 50);
                        }
                        //GET /js/jsapi_ticket/shop_id/150.html HTTP/1.1
                        string methodAndFile = lineConArr[1];
                        if (!string.IsNullOrWhiteSpace(methodAndFile))
                        {
                            string[] methodAndFileArr = methodAndFile.Split(' ');
                            if (methodAndFileArr.Length > 0)
                            {
                                cLogMethod = methodAndFileArr[0];
                                if (cLogMethod.Length > 50) cLogMethod = cLogMethod.Substring(0, 50);
                                cLogFile = methodAndFileArr.Length > 1 ? methodAndFileArr[1] : "";
                                if (cLogFile.Length > 1000) cLogFile = cLogFile.Substring(0, 999);
                                cLogProtocol = methodAndFileArr.Length > 2 ? methodAndFileArr[2] : "";
                                if (cLogProtocol.Length > 50) cLogProtocol = cLogProtocol.Substring(0, 50);
                            }
                        }
                        // 200 437 
                        string statusAndLength = lineConArr[2].Trim();
                        if (!string.IsNullOrWhiteSpace(methodAndFile))
                        {
                            string[] statusAndLengthArr = statusAndLength.Split(' ');
                            if (statusAndLengthArr.Length > 0)
                            {
                                cLogStatus = statusAndLengthArr[0];
                                if (cLogStatus.Length > 50) cLogStatus = cLogStatus.Substring(0, 50);
                                iLogLength = Convert.ToInt32(statusAndLengthArr[1]);
                            }
                        }

                        //https://www.baidu.com/x.html
                        cLogUrl = lineConArr[3];
                        if (cLogUrl.Length > 1000) cLogUrl = cLogUrl.Substring(0, 999);
                        //Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.110 Safari/537.36
                        cLogUserAgent = lineConArr[5];
                        if (cLogUserAgent.Length > 1000) cLogUserAgent = cLogUserAgent.Substring(0, 999);

                        NginxLog nginxLog = new NginxLog();
                        nginxLog.cLogIp = cLogIp;
                        nginxLog.cLogDate = cLogDate;
                        nginxLog.cLogTime = cLogTime;
                        nginxLog.cLogMethod = cLogMethod;
                        nginxLog.cLogFile = cLogFile;
                        nginxLog.cLogProtocol = cLogProtocol;
                        nginxLog.cLogStatus = cLogStatus;
                        nginxLog.iLogLength = iLogLength;
                        nginxLog.cLogUrl = cLogUrl;
                        nginxLog.cLogUserAgent = cLogUserAgent;
                        nginxLog.dCreateTime = DateTime.Now;

                        lsNginxLog.Add(nginxLog);

                    }

                    // Console.ReadKey();
                    if (iCount >= iPageSize)
                    {
                        iPageIndex++;
                        if (lsNginxLog.Count > 0)
                        {
                            InsertBatch(connection, lsNginxLog);
                            lsNginxLog.Clear();
                            Console.WriteLine(iPageIndex + ":" + iPageSize * iPageIndex);
                            iCount = 0;
                        }
                    };
                }

                if (lsNginxLog.Count > 0)
                {
                    InsertBatch(connection, lsNginxLog);
                    lsNginxLog.Clear();
                    iPageIndex++;
                    Console.WriteLine(iPageIndex + ":" + iPageSize * iPageIndex);
                }
            }
        }

        /// <summary>
        /// 批量插入功能
        /// </summary>
        public void InsertBatch<T>(IDbConnection conn, IEnumerable<T> entityList, IDbTransaction transaction = null) where T : class
        {
            var tblName = string.Format("dbo.{0}", typeof(T).Name);
            var tran = (SqlTransaction)transaction;
            using (var bulkCopy = new SqlBulkCopy(conn as SqlConnection, SqlBulkCopyOptions.TableLock, tran))
            {
                bulkCopy.BulkCopyTimeout = 600;
                bulkCopy.BatchSize = entityList.Count();
                bulkCopy.DestinationTableName = tblName;
                var table = new DataTable();
                DapperExtensions.Sql.ISqlGenerator sqlGenerator = new SqlGeneratorImpl(new DapperExtensionsConfiguration());
                var classMap = sqlGenerator.Configuration.GetMap<T>();
                var props = classMap.Properties.Where(x => x.Ignored == false).ToArray();
                foreach (var propertyInfo in props)
                {
                    bulkCopy.ColumnMappings.Add(propertyInfo.Name, propertyInfo.Name);
                    table.Columns.Add(propertyInfo.Name, Nullable.GetUnderlyingType(propertyInfo.PropertyInfo.PropertyType) ?? propertyInfo.PropertyInfo.PropertyType);
                }
                var values = new object[props.Count()];
                foreach (var itemm in entityList)
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = props[i].PropertyInfo.GetValue(itemm, null);
                    }
                    table.Rows.Add(values);
                }
                bulkCopy.WriteToServer(table);
            }
        }

        public string GetSqlConnString()
        {
            if (ConfigurationManager.ConnectionStrings["SqlConnString"] != null) return ConfigurationManager.ConnectionStrings["SqlConnString"].ToString();
            return null;
        }
    }

    public class NginxLog
    {

        public int id;
        public string cLogIp { get; set; }
        public string cLogDate { get; set; }
        public string cLogTime { get; set; }
        public string cLogMethod { get; set; }
        public string cLogFile { get; set; }
        public string cLogProtocol { get; set; }
        public string cLogStatus { get; set; }
        public int iLogLength { get; set; }
        public string cLogUrl { get; set; }
        public string cLogUserAgent { get; set; }
        public DateTime dCreateTime { get; set; }
    }
}
