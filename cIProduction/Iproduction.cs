using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration     ;
using System.Data.Odbc;
using System.Data ;

namespace cIProduction
{
    class Iproduction
    {
        public static void write(DataSet ds, Program.Member usr, DataSet dserrors, OdbcConnection connIS)
        {
            Logger oLogger = new Logger();
            Console.WriteLine("Writting Production to Iseries PDDPDPF");
            SqlDataAdapter adapterTest = new SqlDataAdapter();
            String cmdinsertProduction = "insert into " + usr.idatalib + ".PDDPDPF " +  //DOGG PRODUCTION FILE
                    " (" +  //
                     "PDLEAS," +
                     "PDRPYY," +
                     "PDRPMM," +
                     "PDRPDD," +
                     "PDOBBL, " +//5
                     "PDGMCF, " +//6
                     "PDWATR," +
                     "TTLTK," + //8
                     "OILMTR," +
                     "PDTUBP,"+
                     "PDCASP," +
                     "PDCHOK," +
                     "PDSPM," +
                     "PDRMKS," +
                     "BATT," +
                     "COM, " + 
                     "SYS" + //17
                      " )" +
                      " values(" +  //must insert in order of fields
                     "?," +
                     "?," +
                     "?," +
                     "?," +
                     "?," +
                     "?, " +
                     "?," +
                     "?," +
                     "?," +
                     "?," +
                      "?," +
                      "?," +
                     "?," +
                     "?," +
                     "?," +
                      "?," +
                     "?" +   //17
                     ")";
            try
            {
                OdbcCommand dltIprod = new OdbcCommand("delete from " + usr.outtable,connIS);
                  dltIprod.ExecuteNonQuery();
                 int WriteCount = 0;
                OdbcCommand writeProduction = new OdbcCommand(cmdinsertProduction, connIS);
                foreach (DataTable DOGGProduction in ds.Tables) // only one table,  but as an example for reading all tables in ds
                {
                    //must insert in order of fields
                    foreach (DataRow ProdRow in DOGGProduction.Rows)
                    {   //format each column of DOGG  for Iseries
                        try
                        {
                            writeProduction.Parameters.Clear();
                            writeProduction.Parameters.AddWithValue("@PDLEAS", ProdRow.Field<string>("LeaseNumber"));
                            int runyy = DateIBM.MStoIBMDateYY(Convert.ToDateTime((ProdRow["ProductionDate"].ToString()))) ;
                            int runmm = DateIBM.MStoIBMDateMM(Convert.ToDateTime((ProdRow["ProductionDate"].ToString())));
                            int rundd = DateIBM.MStoIBMDateDD(Convert.ToDateTime((ProdRow["ProductionDate"].ToString())));
                            int PRRPDT = runmm * 10000 + rundd * 100 + runyy;
                            writeProduction.Parameters.AddWithValue("@PDRPYY", runyy);
                            writeProduction.Parameters.AddWithValue("@PDRPMM", runmm);
                            writeProduction.Parameters.AddWithValue("@PDRPDD", rundd);
                            writeProduction.Parameters.AddWithValue("@PDOBBL",  Convert.ToDecimal((ProdRow["OilProduction"].ToString())));// 
                            writeProduction.Parameters.AddWithValue("@PDGMCF",  Convert.ToDecimal((ProdRow["GasProduction"].ToString())));//gas
                            writeProduction.Parameters.AddWithValue("@PDWATR",  Convert.ToDecimal((ProdRow["WaterProduction"].ToString())));// 
                            writeProduction.Parameters.AddWithValue("@TTLTK", Convert.ToDecimal((ProdRow["OilSales"].ToString())));  //sales
                            writeProduction.Parameters.AddWithValue("@OILMTR", Convert.ToDecimal(0.0)); //18
                            writeProduction.Parameters.AddWithValue("@PDTUBP", Convert.ToDecimal((ProdRow["TubingPressure"].ToString())));
                            writeProduction.Parameters.AddWithValue("@PDCASP",Convert.ToDecimal((ProdRow["CasingPressure"].ToString()))); //20
                            double ChokeDouble = 0.0d;
                            bool result = Double.TryParse(ProdRow["ChokeSize"].ToString(), out ChokeDouble);
                            decimal ChokeDecimal = 0.0m;
                            if (result)
                                ChokeDecimal = Convert.ToDecimal(ChokeDouble);
                            writeProduction.Parameters.AddWithValue("@PDCHOK", ChokeDecimal);
                            //writeProduction.Parameters.AddWithValue("@PDSPM", 0.0m);
                            decimal strokesper = Convert.ToDecimal((ProdRow["StrokesPerMinute"].ToString()));
                            strokesper =  Math.Round(strokesper, 1, MidpointRounding.AwayFromZero); // Rounds "up"
	                        writeProduction.Parameters.AddWithValue("@PDSPM", 	strokesper); 
                            if (ProdRow["Remarks"].ToString().Length < 1 || ProdRow["Remarks"].ToString().Length > 50)
                                writeProduction.Parameters.AddWithValue("@PDRMKS", " "); //1 
                            else
                                writeProduction.Parameters.AddWithValue("@PDRMKS", ProdRow["Remarks"].ToString()); //
                            writeProduction.Parameters.AddWithValue("@BATT", ProdRow.Field<string>("LeaseNumber"));
                            writeProduction.Parameters.AddWithValue("@COM", " "); //16
                             writeProduction.Parameters.AddWithValue("@SYS", "DOGG");  
                            writeProduction.ExecuteNonQuery();
                            WriteCount += 1;
                        }
                        catch (Exception DBserror)
                        {
                            oLogger.logErrorToLog(DBserror.Message, usr.pgmname, usr.logpath);
                            //                                Lease                          tank                         date ticket
                            IseriesErrors.AddRowM4(dserrors, ProdRow["LeaseNumber"].ToString(),"No Tank", (Convert.ToDateTime((ProdRow["ProductionDate"].ToString()))),DBserror.ToString(),"Insert Err:",  usr.pgmname, usr.logpath);
                        }
                    }
                }
                writeProduction.Dispose();
                oLogger.logErrorToLog("NAR Wrote Iprod:" + WriteCount.ToString(), usr.pgmname, usr.logpath);
            }
            catch (Exception DBserror)
            {
                oLogger.logErrorToLog(DBserror.Message, usr.pgmname, usr.logpath);

            }
        }
    }

}
