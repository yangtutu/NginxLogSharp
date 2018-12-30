using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace NginxLogSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime dtStart = DateTime.Now;
            try
            {
                new DoWork();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            DateTime dtEnd = DateTime.Now;
            string nowTime = (dtEnd - dtStart).Milliseconds.ToString("F0");
            Console.WriteLine("time:" + nowTime);

            Console.ReadKey();
        }




    }

}
