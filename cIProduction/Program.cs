//write production to I series P

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration     ;
using System.Data.Odbc;
using System.Data;

namespace cIProduction
{
    class Program
    {
        static void Main(string[] args)
        {

            Member usr = new Member();
            var strODBConn = ConfigurationManager.ConnectionStrings["ODBConn"].ToString();  // iseires ODBC COnnection
           
            OdbcConnection connIS = new OdbcConnection(strODBConn);
            DataSet ds = new DataSet();
            DataSet dserrors = new DataSet();
            try
            {
                cIProduction.IseriesErrors.TableBuild(dserrors); //.TableBuild(dserrors);
                Logger oLogger = new Logger();
                oLogger.createLogFile("Process Started, days look back=" + usr.daysback, usr.PGMNM, usr.logpath);
                connIS.Open();
                string sqlsv = usr.sqlserver; ;
                SqlConnection DOGGConn = new SqlConnection(@"Data Source=" + sqlsv + "; Initial Catalog = " + usr.outdb + " ; Integrated Security=true");
                DOGGConn.Open();
                IseriesErrors.AddRowM4(dserrors, "-", "-", DateTime.Today, "Starting:" + usr.pgmname, "-", usr.pgmname, usr.logpath);
                int daysback = Convert.ToInt32(usr.daysback);
                TimeSpan daterange = new TimeSpan(daysback, 0, 0, 0, 0);  //days back to load  
                DateTime dtBackDate = DateTime.Today.Subtract(daterange);
                string stBackDate = dtBackDate.ToString("MM/dd/yy");
                string strYYYYMM = (dtBackDate.Year * 100 + dtBackDate.Month).ToString();
                DateTime dtfldBackDate = DateTime.Today.Subtract(daterange);  //date format must by '2013-01-01 00:00:00'
                System.DateTime.Today.ToString("MM/dd/yy");
                // all Production tables
                string strSelectProduction = "select * from DailyProductionTransfer where ProductionDate >= '" + stBackDate + "' and  TransferDirection = 1";
                // only select DOGG entered tickets
                SqlCommand findTProductions = new SqlCommand(strSelectProduction, DOGGConn);
                SqlDataAdapter ProductionTable = new SqlDataAdapter(strSelectProduction, DOGGConn);
                ProductionTable.Fill(ds, "DOGGProduction");
                cIProduction.Iproduction.write(ds, usr, dserrors, connIS);  //actual write of tickets to Isereis
                IseriesErrors.Write(dserrors, usr.ilibfil, connIS, usr.pgmname, usr.logpath);
                ds.Dispose();
                dserrors.Dispose();
                if (usr.delDOGG == "yes")
                {
                    oLogger.logErrorToLog("Production Deleted from DOGG:" + ds.Tables[0].Rows.Count.ToString(), usr.PGMNM, usr.logpath);
                    SqlCommand dltTickets = new SqlCommand("delete from DailyProductionTransfer where TransferDirection = 1");
                    dltTickets.ExecuteNonQuery();
                }
                DOGGConn.Close();
                DOGGConn.Dispose();
                connIS.Close();
                connIS.Dispose();
                oLogger.logErrorToLog("day Production Read from DOGG:" + ds.Tables[0].Rows.Count.ToString(), usr.PGMNM, usr.logpath);
            }
            catch (Exception DBserror)
            {
                Logger oLogger = new Logger();
                oLogger.logErrorToLog(DBserror.Message, usr.pgmname, usr.logpath);
                IseriesErrors.Write(dserrors, usr.ilibfil, connIS, usr.pgmname, usr.logpath);
                connIS.Close();
                connIS.Dispose();
            }
        }


        public class Member  //iseries user
        {
            public string logpath  //path to store file
            { get; set; }
            public string datasource  //path 
            { get; set; }
            public string profile //user profile for iseries, entered
            { get; set; }
            public string password //user password for iseries, entered
            { get; set; }
            public string defaultcollection  //user password for iseries, entered
            { get; set; }
            public string LibraryList //user library list
            { get; set; }
            public string xmlpathname//user library list
            { get; set; }
            public string constr //user connection string
            { get; set; }
            public decimal pctcomplete //user connection string
            { get; set; }
            public string logname //set in pgm
            { get; set; }
            public string idatalib //set in pgm
            { get; set; }
            public string outtable//set in pgm
            { get; set; }
            public string sqlserver //set in pgm
            { get; set; }
            public string Ierrors //set in pgm
            { get; set; }
            public string daysback //set in pgm
            { get; set; }
            public string ODBConn //set in pgm
            { get; set; }
            public string TicketTable //set in pgm
            { get; set; }
            public string outdb //set in pgm
            { get; set; }
            public string outTable //set in pgm
            { get; set; }
            public string PGMNM //set in pgm
            { get; set; }
            public string ProdConn //set in pgm
            { get; set; }
            public string pgmname //set in pgm
            { get; set; }
            public string ilibfil //set in pgm
            { get; set; }
            public string infile //set in pgm
            { get; set; }
            public string delDOGG //set in pgm
            { get; set; }
            public Member()
            {
                logpath = ConfigurationManager.AppSettings["logpath"];
                logname = ConfigurationManager.AppSettings["logname"];
                password = ConfigurationManager.AppSettings["password"];
                profile = ConfigurationManager.AppSettings["profile"];
                datasource = ConfigurationManager.AppSettings["iseriesip"];
                idatalib = ConfigurationManager.AppSettings["idatalibrary"];
                outtable = ConfigurationManager.AppSettings["outtable"];
                sqlserver = ConfigurationManager.AppSettings["sqlserver"];
                Ierrors = ConfigurationManager.AppSettings["Iserieserrors"];
                daysback = ConfigurationManager.AppSettings["daysback"];
                ODBConn = ConfigurationManager.AppSettings["ODBConn"];
                ProdConn = ConfigurationManager.ConnectionStrings["ProdConnection"].ToString();
                TicketTable = ConfigurationManager.AppSettings["TicketTable"];
                outdb = ConfigurationManager.AppSettings["outdb"];
                outTable = ConfigurationManager.AppSettings["outTable"];
                PGMNM = ConfigurationManager.AppSettings["PGMNM"];
                pgmname = ConfigurationManager.AppSettings["PGMNM"];
                infile = ConfigurationManager.AppSettings["infile"];
                ilibfil = ConfigurationManager.AppSettings["ilibfil"]; // error report file on Iseries
                delDOGG = ConfigurationManager.AppSettings["delDOGG"];
            }
        }
    }
}