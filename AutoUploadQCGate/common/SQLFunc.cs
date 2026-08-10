using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DefaultNS.Common
{
    class msSQL
    {
        public static string Username = "";
        //public static string Role = ""; // Bien chung luu tru Quyen han cua User hien tai

        public static string DBHostName; // 
        public static string DBPort; // 
        public static string DBName; // 
        public static string DBUserName; // 
        public static string DBPasswordKey; //
                                            //public static string DBPassword; //

        public static string DBHostWebInput;
        public static string DBPortWebInput;
        public static string DBNameWebInput;
        public static string DBUserWebInput;
        public static string DBPasswordWebInput;

        public static string DBNameVCDLink;

        public static void InitGlobalVarial()
        {
            //IniFile ini = new IniFile(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\AppConfig.ini");
            //DBHostName = "";
            //DBPort = "";
            //DBName = "";
            //DBUserName = ini.IniReadValue("DATABASE", "DBUser", "sa");
            //DBPasswordKey = ini.IniReadValue("DATABASE", "DBPass", "pmttAU2019");

            //DBHostWebInput = ini.IniReadValue("DATABASE", "DBHostWebInput", "10.126.19.3");
            //DBPortWebInput = ini.IniReadValue("DATABASE", "DBPortWebInput", "1433");
            //DBNameWebInput = ini.IniReadValue("DATABASE", "DBNameWebInput", "PismoWebInput");
            //DBUserWebInput = ini.IniReadValue("DATABASE", "DBUserWebInput", "sa");
            //DBPasswordWebInput = ini.IniReadValue("DATABASE", "DBPassWebInput", "seev@123;");

            ////DBPasswordKey = Global.Encrypt(DBUserName + DBPasswordKey).Replace("-", "");
            ////InitThreadPollingDB();


            //bool issOK =  TestConnection();

        }


        public static string connStringPismoShopFloor
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=" + DBHostWebInput + "," + DBPortWebInput + "; Initial Catalog=" + "PISMO_ShopFloor" + ";User Id=" + DBUserWebInput + ";Password=" + DBPasswordWebInput + ";Connection Timeout=2"; }
        }

        public static string connStringWebInput
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=" + DBHostWebInput + "," + DBPortWebInput + "; Initial Catalog=" + DBNameWebInput + ";User Id=" + DBUserWebInput + ";Password=" + DBPasswordWebInput + ";Connection Timeout=2"; }
        }

        public static string connString
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=" + DBHostName + "," + DBPort + "; Initial Catalog=" + DBName + ";User Id=" + DBUserName + ";Password=" + DBPasswordKey + ";Connection Timeout=2"; }
        }

        public static string connStringPISMO_CommonMaster
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=" + DBHostName + "," + DBPort + "; Initial Catalog=" + "PISMO_CommonMaster" + ";User Id=" + DBUserName + ";Password=" + DBPasswordKey + ";Connection Timeout=2"; }
        }
        public static string connStringPISMO_Trace
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=" + DBHostName + "," + DBPort + "; Initial Catalog=" + "PISMO_TraceDB" + ";User Id=" + DBUserName + ";Password=" + DBPasswordKey + ";Connection Timeout=2"; }
        }

        public static string connStringAOI_Trace
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=" + DBHostName + "," + DBPort + "; Initial Catalog=" + "AOI_F1_DB" + ";User Id=" + DBUserName + ";Password=" + DBPasswordKey + ";Connection Timeout=2"; }
        }
        public static string connStringPMTT_TRACE_F5
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=" + DBHostName + "," + DBPort + "; Initial Catalog=" + "PMTT-TRACE-F5" + ";User Id=" + DBUserName + ";Password=" + DBPasswordKey + ";Connection Timeout=2"; }
        }


        //public static string connStringVCDLink
        //{
        //    //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
        //    //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
        //    get { return "Network Library=DBMSSOCN; Data Source=" + DBHostIDLink + "," + DBPortIDLink + "; Initial Catalog=" + DBNameVCDLink + ";User Id=" + DBUserIDLink + ";Password=" + DBPasswordIDLink + ";"; }
        //}

        public static string connStringBoardTest
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=10.212.7.124,1433; Initial Catalog=BoardRegistration_F4;User Id=sa;Password=seev@123;"; }
        }
        public static string connStringIDLinkTest
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=10.212.7.124,1433; Initial Catalog=SEI-VCDTraceDB-NM4;User Id=sa;Password=seev@123;"; }
        }

        public static string connPMTT_Test
        {
            //get { return @"Data Source=DESKTOP-ADL3ETG\SQLEXPRESS;Initial Catalog=TEST-GradeCheckFVI;Integrated Security = True"; }
            //MySQL: get { return "server=" + DBHostName + "; Port=" + DBPort + "; User Id=" + DBUserName + ";Password=" + DBPassword + ";Persist Security Info=True;database=" + DBName + ";Character Set=utf8;Connection Timeout=10;Command Timeout=40;"; }
            get { return "Network Library=DBMSSOCN; Data Source=erp.pmtt.com.vn,11433; Initial Catalog=PMTT-TRACE-F4;User Id=seev;Password=seev@123;"; }
        }


        public static bool TestConnection()
        {
            //InitGlobalVarial();
            bool bCommOK = false;


            SqlConnection myConnection = new SqlConnection(connString);
            try
            {
                myConnection.Open();
                myConnection.Close();
                bCommOK = true;
                return bCommOK;
            }
            catch (SqlException ex)
            {
                
            }
            finally
            {
                myConnection.Dispose();
            }

            return bCommOK;

        }
        public static bool TestConnection(string connStringInput)
        {
            //InitGlobalVarial();
            bool bCommOK = false;


            SqlConnection myConnection = new SqlConnection(connStringInput);
            try
            {
                myConnection.Open();
                myConnection.Close();
                bCommOK = true;
                return bCommOK;
            }
            catch (SqlException ex)
            {
               
            }
            finally
            {
                myConnection.Dispose();
            }

            return bCommOK;

        }
        public static bool TestConnectionWebInput()
        {
            //InitGlobalVarial();
            bool bCommOK = false;


            SqlConnection myConnection = new SqlConnection(connStringWebInput);
            try
            {
                myConnection.Open();
                myConnection.Close();
                bCommOK = true;
                return bCommOK;
            }
            catch (SqlException ex)
            {
                //Global.WriteLogFile("[TestConnection] - " + ex.ToString());
            }
            finally
            {
                myConnection.Dispose();
            }

            return bCommOK;

        }

        public static bool TestConnectionTraceF5()
        {
            //InitGlobalVarial();
            bool bCommOK = false;


            SqlConnection myConnection = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                myConnection.Open();
                myConnection.Close();
                bCommOK = true;
                return bCommOK;
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[TestConnection] - " + ex.ToString());
            }
            finally
            {
                myConnection.Dispose();
            }

            return bCommOK;

        }

        public static bool TestConnectionPismo()
        {
            //InitGlobalVarial();
            bool bCommOK = false;


            SqlConnection myConnection = new SqlConnection(connStringPISMO_CommonMaster);
            try
            {
                myConnection.Open();
                myConnection.Close();
                bCommOK = true;
                return bCommOK;
            }
            catch (SqlException ex)
            {
                //Global.WriteLogFile("[TestConnection] - " + ex.ToString());
            }
            finally
            {
                myConnection.Dispose();
            }

            return bCommOK;

        }

        public static bool ExecuteNonQuery(string query_string)
        {
            //InitGlobalVarial();
            bool bCommOK = false;
            //Global.WriteLogFile("[ExecuteNonQuery] - " + "\r\nQueryString: " + query_string);
            SqlConnection myConnection = new SqlConnection(connString);
            try
            {
                myConnection.Open();
                using (SqlCommand myCommand = new SqlCommand(query_string, myConnection))
                {
                    myCommand.ExecuteNonQuery();
                }
                myConnection.Close();
                bCommOK = true;
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteNonQuery] - " + ex.ToString() + "\r\n" + query_string);
                //if (ex.Number == 2627) //Violation of %ls constraint '%.*ls'. Cannot insert duplicate key in object '%.*ls'.
                //    MessageBox.Show(ex.Message);
            }
            finally
            {
                myConnection.Dispose();
            }
            return bCommOK;
        }

        public static bool ExecuteNonQuery(string query_string, params SqlParameter[] parameters)
        {
            bool success = false;
            using (SqlConnection connection = new SqlConnection(connString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query_string, connection))
                    {
                        if (parameters != null)
                            command.Parameters.AddRange(parameters);
                        command.ExecuteNonQuery();
                    }
                    success = true;
                }
                catch (SqlException ex)
                {
                    Global.WriteLogFile("[ExecuteNonQuery(parameterized)] - " + ex);
                }
            }
            return success;
        }

        public static int ExecuteNonQueryCount(string query_string, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query_string, connection))
                    {
                        if (parameters != null)
                            command.Parameters.AddRange(parameters);

                        return command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Global.WriteLogFile("[ExecuteNonQueryCount(parameterized)] - " + ex);
                    return -1;
                }
            }
        }

        public static bool ExecuteNonQueryF5(string query_string)
        {
            //InitGlobalVarial();
            bool bCommOK = false;
            //Global.WriteLogFile("[ExecuteNonQuery] - " + "\r\nQueryString: " + query_string);
            SqlConnection myConnection = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                myConnection.Open();
                using (SqlCommand myCommand = new SqlCommand(query_string, myConnection))
                {
                    myCommand.ExecuteNonQuery();
                }
                myConnection.Close();
                bCommOK = true;
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteNonQuery] - " + ex.ToString() + "\r\n" + query_string);
                //if (ex.Number == 2627) //Violation of %ls constraint '%.*ls'. Cannot insert duplicate key in object '%.*ls'.
                //    MessageBox.Show(ex.Message);
            }
            finally
            {
                myConnection.Dispose();
            }
            return bCommOK;
        }

        public static DataTable ExecuteDataTable(string query_string)
        {
            //InitGlobalVarial();
            DataTable data_table = new DataTable();
            SqlConnection myConnection = new SqlConnection(connString);
            try
            {
                myConnection.Open();
                SqlDataAdapter myAdapter = new SqlDataAdapter(query_string, myConnection);
                try
                {
                    myAdapter.Fill(data_table);
                }
                catch (SqlException ex)
                {
                    Global.WriteLogFile("[ExecuteDataTable/myAdapter.Fill(data_table)] - " + ex.ToString());
                }
                myConnection.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteDataTable] - " + ex.ToString());
            }
            finally
            {
                myConnection.Dispose();
            }
            return data_table;
        }

        public static DataTable ExecuteDataTable(string query_string, params SqlParameter[] parameters)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query_string, connection))
                    {
                        if (parameters != null)
                            command.Parameters.AddRange(parameters);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                            adapter.Fill(dataTable);
                    }
                }
                catch (SqlException ex)
                {
                    Global.WriteLogFile("[ExecuteDataTable(parameterized)] - " + ex);
                }
            }
            return dataTable;
        }

        public static DataTable ExecuteDataTableF5(string query_string)
        {
            //InitGlobalVarial();
            DataTable data_table = new DataTable();
            SqlConnection myConnection = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                myConnection.Open();
                SqlDataAdapter myAdapter = new SqlDataAdapter(query_string, myConnection);
                try
                {
                    myAdapter.Fill(data_table);
                }
                catch (SqlException ex)
                {
                    Global.WriteLogFile("[ExecuteDataTable/myAdapter.Fill(data_table)] - " + ex.ToString());
                }
                myConnection.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteDataTable] - " + ex.ToString());
            }
            finally
            {
                myConnection.Dispose();
            }
            return data_table;
        }
        public static object ExecuteScalar(string query_string)
        {
            InitGlobalVarial();
            object data_obj = null;
            SqlConnection myConnection = new SqlConnection(connString);
            try
            {
                myConnection.Open();
                SqlCommand myCommand = new SqlCommand(query_string, myConnection);
                data_obj = myCommand.ExecuteScalar();
                myConnection.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                myConnection.Dispose();
            }
            return data_obj;
        }
        public static object ExecuteScalarF5(string query_string)
        {
            InitGlobalVarial();
            object data_obj = null;
            SqlConnection myConnection = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                myConnection.Open();
                SqlCommand myCommand = new SqlCommand(query_string, myConnection);
                data_obj = myCommand.ExecuteScalar();
                myConnection.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                myConnection.Dispose();
            }
            return data_obj;
        }

        public static DataSet ExecuteDataSetSP(string proc_name, string pname1 = "", object pvalue1 = null, string pname2 = "", object pvalue2 = null, string pname3 = "", object pvalue3 = null, string pname4 = "", object pvalue4 = null, string pname5 = "", object pvalue5 = null, string pname6 = "", object pvalue6 = null, string pname7 = "", object pvalue7 = null, string pname8 = ""
           , object pvalue8 = null, string pname9 = "", object pvalue9 = null, string pname10 = "", object pvalue10 = null, string pname11 = "", object pvalue11 = null, string pname12 = "", object pvalue12 = null, string pname13 = "", object pvalue13 = null, string pname14 = "", object pvalue14 = null
           , string pname15 = "", object pvalue15 = null, string pname16 = "", object pvalue16 = null, string pname17 = "", object pvalue17 = null, string pname18 = "", object pvalue18 = null, string pname19 = "", object pvalue19 = null, string pname20 = "", object pvalue20 = null)
        {
            DataSet data_set = new DataSet();
            try
            {
                SqlConnection myConnection = new SqlConnection(connString);
                try
                {
                    SqlCommand command = new SqlCommand(proc_name, myConnection);
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    if (pname1 != "") command.Parameters.AddWithValue(pname1, pvalue1);
                    if (pname2 != "") command.Parameters.AddWithValue(pname2, pvalue2);
                    if (pname3 != "") command.Parameters.AddWithValue(pname3, pvalue3);
                    if (pname4 != "") command.Parameters.AddWithValue(pname4, pvalue4);
                    if (pname5 != "") command.Parameters.AddWithValue(pname5, pvalue5);
                    if (pname6 != "") command.Parameters.AddWithValue(pname6, pvalue6);
                    if (pname7 != "") command.Parameters.AddWithValue(pname7, pvalue7);
                    if (pname8 != "") command.Parameters.AddWithValue(pname8, pvalue8);
                    if (pname9 != "") command.Parameters.AddWithValue(pname9, pvalue9);
                    if (pname10 != "") command.Parameters.AddWithValue(pname10, pvalue10);
                    if (pname11 != "") command.Parameters.AddWithValue(pname11, pvalue11);
                    if (pname12 != "") command.Parameters.AddWithValue(pname12, pvalue12);
                    if (pname13 != "") command.Parameters.AddWithValue(pname13, pvalue13);
                    if (pname14 != "") command.Parameters.AddWithValue(pname14, pvalue14);
                    if (pname15 != "") command.Parameters.AddWithValue(pname15, pvalue15);
                    if (pname16 != "") command.Parameters.AddWithValue(pname16, pvalue16);
                    if (pname17 != "") command.Parameters.AddWithValue(pname17, pvalue17);
                    if (pname18 != "") command.Parameters.AddWithValue(pname18, pvalue18);
                    if (pname19 != "") command.Parameters.AddWithValue(pname19, pvalue19);
                    if (pname20 != "") command.Parameters.AddWithValue(pname20, pvalue20);
                    myConnection.Open();
                    try
                    {
                        new SqlDataAdapter(command).Fill(data_set);
                    }
                    catch (SqlException ex)
                    {
                        Global.WriteLogFile("[ExecuteDataSetSP/myAdapter.Fill(data_set)] - " + ex.ToString());
                    }
                    myConnection.Close();
                }
                catch (SqlException ex)
                {
                    Global.WriteLogFile("[ExecuteDataSetSP] - " + ex.ToString());
                }
                finally
                {
                    myConnection.Dispose();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteDataSetSP/myAdapter.Fill(data_table)] - " + ex.ToString());
            }
            return data_set;
        }

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////////QC GATE/////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="procedure"></param>
        /// <param name="EmapID"></param>
        /// <returns></returns>

        public static object ExecuteScalarPismoComon(string query_string)
        {
            object data_obj = null;
            SqlConnection myConnection = new SqlConnection(connStringPISMO_CommonMaster);
            try
            {
                myConnection.Open();
                SqlCommand myCommand = new SqlCommand(query_string, myConnection);
                data_obj = myCommand.ExecuteScalar();
                myConnection.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                myConnection.Dispose();
            }
            return data_obj;
        }

        public static object ExecuteScalarPismoShopFloor(string query_string)
        {
            object data_obj = null;
            SqlConnection myConnection = new SqlConnection(connStringPismoShopFloor);
            try
            {
                myConnection.Open();
                SqlCommand myCommand = new SqlCommand(query_string, myConnection);
                data_obj = myCommand.ExecuteScalar();
                myConnection.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                myConnection.Dispose();
            }
            return data_obj;
        }


        public static DataTable QCGate_GetDefineBlock_ByEmapID(string procedure, string EmapID)
        {
            //object data_obj = null;
            DataTable dtab = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_CommonMaster))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@EMapID_A", SqlDbType.VarChar).Value = EmapID;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);

                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);
                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetErrMap_ByBlockPkid(string procedure, int blockPkid)
        {
            //object data_obj = null;
            DataTable dtab = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@BlockPkid", SqlDbType.Int).Value = blockPkid;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);

                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);
                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }


        public static DataTable QCGate_GetAviSusData_ByBlockPkid(string procedure, int blockPkid, int blockIdx)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@BlockPkid", SqlDbType.Int).Value = blockPkid;
                    cmd.Parameters.Add("@BlockIndex", SqlDbType.Int).Value = blockIdx;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);

                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);
                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetAviSusData_ByBlockID(string procedure, string blockId, int blockIdx)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@BlockId", SqlDbType.VarChar).Value = blockId;
                    cmd.Parameters.Add("@BlockIndex", SqlDbType.Int).Value = blockIdx;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);

                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);
                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetAOITraceData_ByBlockID(string procedure, string blockID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringAOI_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@BlockID", SqlDbType.VarChar).Value = blockID;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);

                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);
                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetAOITraceData_ByProductID(string procedure, DataTable dtab1)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringAOI_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@tbProductIDs";
                    param.Value = dtab1;
                    cmd.Parameters.Add(param);
                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetAOIViaData_ByBlockID(string procedure, string blockID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@BlockID", SqlDbType.VarChar).Value = blockID;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);

                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);
                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetECheckData_ByBlockPkid(string procedure, int blockPkid, int blockIdx)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@BlockPkid", SqlDbType.Int).Value = blockPkid;
                    cmd.Parameters.Add("@BlockIndex", SqlDbType.Int).Value = blockIdx;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);

                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);
                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }

        public static DataTable QCGate_GetAllDataPrevStage_ByProductID(string procedure, DataTable dtab1)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@tbProductIDs";
                    param.Value = dtab1;
                    cmd.Parameters.Add(param);
                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetBlockPkid_ByProductID(string procedure, DataTable dtab1)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@tbProductIDs";
                    param.Value = dtab1;
                    cmd.Parameters.Add(param);

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }

        public static DataTable QCGate_GetMQCData_ByProductID(string procedure, DataTable dtab1, string blockID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringWebInput))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@tbProductIDs";
                    param.Value = dtab1;
                    cmd.Parameters.Add(param);

                    cmd.Parameters.Add("@BlockId", SqlDbType.NVarChar, 450).Value = blockID;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }

        public static DataTable QCGate_GetMQCData_ByBlockID2(string procedure, string blockID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringWebInput))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@BlockId", SqlDbType.NVarChar, 450).Value = blockID;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetMQCData_ByEmapID2(string procedure, string emapID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringWebInput))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@EMapId", SqlDbType.NVarChar, 450).Value = emapID;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }

        public static DataTable QCGate_GetItemcodeAndLot_ByBlockPkid(string procedure, DataTable dtab1)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_CommonMaster))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@tbBlockPkids";
                    param.Value = dtab1;
                    cmd.Parameters.Add(param);
                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    //data_obj = cmd.ExecuteScalar();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }


        public static object QCGate_CheckDupplicate_ByPcsID(string procedure, string pcsID)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                SqlCommand cmd = new SqlCommand(procedure, conn);
                cmd.CommandType = CommandType.StoredProcedure; ;
                cmd.Parameters.Add("@PcsID", SqlDbType.VarChar).Value = pcsID;

                conn.Open();
                data_obj = cmd.ExecuteScalar();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }
        public static object QCGate_GetEmapGrade_ByEmapID(string procedure, string emapID)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connStringPISMO_Trace);
            try
            {
                SqlCommand cmd = new SqlCommand(procedure, conn);
                cmd.CommandType = CommandType.StoredProcedure; ;
                cmd.Parameters.Add("@PanelID", SqlDbType.VarChar).Value = emapID;

                conn.Open();
                data_obj = cmd.ExecuteScalar();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }


        public static object QCGate_GetQCGateID_ByAlbagID(string procedure, string albagID, string indication, string program, string machineID, string operatorID)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connStringPISMO_Trace);
            try
            {
                SqlCommand cmd = new SqlCommand(procedure, conn);
                cmd.CommandType = CommandType.StoredProcedure; ;
                cmd.Parameters.Add("@AlbagID", SqlDbType.VarChar).Value = albagID;
                cmd.Parameters.Add("@Indication", SqlDbType.VarChar).Value = indication;
                cmd.Parameters.Add("@ProgramName", SqlDbType.VarChar).Value = program;
                cmd.Parameters.Add("@MachineID", SqlDbType.VarChar).Value = machineID;
                cmd.Parameters.Add("@OperatorID", SqlDbType.VarChar).Value = operatorID;

                conn.Open();
                data_obj = cmd.ExecuteScalar();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }

        public static object QCGate_SaveDataDetail(string procedure, string albagID, int panelNum, int pcsNum, int pcsOkNum, int pcsNgNum, DataTable dtabSave)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connStringPISMO_Trace);
            try
            {
                SqlCommand cmd = new SqlCommand(procedure, conn);
                cmd.CommandType = CommandType.StoredProcedure; ;

                SqlParameter param = new SqlParameter();
                param.ParameterName = "@tbDataSave";
                param.Value = dtabSave;
                cmd.Parameters.Add(param);

                cmd.Parameters.Add("@AlbagID", SqlDbType.VarChar).Value = albagID;
                cmd.Parameters.Add("@PanelNumber", SqlDbType.Int).Value = panelNum;
                cmd.Parameters.Add("@PcsNumber", SqlDbType.Int).Value = pcsNum;
                cmd.Parameters.Add("@PcsOK", SqlDbType.Int).Value = pcsOkNum;
                cmd.Parameters.Add("@PcsNG", SqlDbType.Int).Value = pcsNgNum;

                conn.Open();
                data_obj = cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }


        public static object QCGate_SaveDataDetail(string procedure, int resultPanel, string albagID, string panelID, string indication, string machineID, string programname, string operatorID, int panelNum, int pcsNum, int pcsOkNum, int pcsNgNum, DataTable dtabSave)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                SqlCommand cmd = new SqlCommand(procedure, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter param = new SqlParameter();
                param.ParameterName = "@tbDataSave";
                param.Value = dtabSave;
                cmd.Parameters.Add(param);

                cmd.Parameters.Add("@ResultPanel", SqlDbType.Int).Value = resultPanel;
                cmd.Parameters.Add("@AlbagID", SqlDbType.VarChar).Value = albagID;
                cmd.Parameters.Add("@PanelID", SqlDbType.VarChar).Value = panelID;
                cmd.Parameters.Add("@Indication", SqlDbType.VarChar).Value = indication;
                cmd.Parameters.Add("@MachineID", SqlDbType.VarChar).Value = machineID;
                cmd.Parameters.Add("@ProgramName", SqlDbType.VarChar).Value = programname;
                cmd.Parameters.Add("@OperatorID", SqlDbType.VarChar).Value = operatorID;
                cmd.Parameters.Add("@PanelNumber", SqlDbType.Int).Value = panelNum;
                cmd.Parameters.Add("@PcsNumber", SqlDbType.Int).Value = pcsNum;
                cmd.Parameters.Add("@PcsOK", SqlDbType.Int).Value = pcsOkNum;
                cmd.Parameters.Add("@PcsNG", SqlDbType.Int).Value = pcsNgNum;

                conn.Open();
                data_obj = cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }

        public static DataTable QCGate_GetCurrentDataDetail_ByIndicationID(string procedure, string albagID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5))
                {
                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@AlbagID", SqlDbType.VarChar).Value = albagID;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetDataDetail_ByIndicationID(string procedure, string indication)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5))
                {
                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@Indication", SqlDbType.VarChar).Value = indication;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }
        public static DataTable QCGate_GetShipDataDetail_ByIndicationID(string procedure, string indication)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5))
                {
                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@Indication", SqlDbType.VarChar).Value = indication;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }

        public static DataTable QCGate_GetQCGateInfo_ByAlbagID(string procedure, string AlbagID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPISMO_Trace))
                {

                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@PcsNG", SqlDbType.VarChar).Value = AlbagID;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }







        public static DataTable AutoTransfer_TraceDataByTimeAndMachineID(string procedure, string TimeStart, string TimeEnd, string MachineID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    using (SqlCommand cmd = new SqlCommand(procedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        //cmd.Parameters.Add("@MachineID", SqlDbType.VarChar).Value = MachineID;
                        cmd.Parameters.Add("@TimeStart", SqlDbType.NVarChar).Value = TimeStart;
                        cmd.Parameters.Add("@TimeEnd", SqlDbType.NVarChar).Value = TimeEnd;
                        cmd.Parameters.Add("@MachineID", SqlDbType.NVarChar).Value = MachineID;

                        SqlDataAdapter adp = new SqlDataAdapter(cmd);

                        conn.Open();
                        //data_obj = cmd.ExecuteScalar();
                        adp.Fill(dtab);
                        conn.Close();
                    }

                }
            }
            catch (SqlException ex)
            {
                dtab = null;
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }

        public static DataTable AutoTransfer_TraceDataDetailByAutoTransferID(string procedure, string AutoTransferID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    using (SqlCommand cmd = new SqlCommand(procedure, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        //cmd.Parameters.Add("@MachineID", SqlDbType.VarChar).Value = MachineID;
                        cmd.Parameters.Add("@AutoTransferID", SqlDbType.NVarChar).Value = AutoTransferID;
                        SqlDataAdapter adp = new SqlDataAdapter(cmd);

                        conn.Open();
                        //data_obj = cmd.ExecuteScalar();
                        adp.Fill(dtab);
                        conn.Close();
                    }

                }
            }
            catch (SqlException ex)
            {
                dtab = null;
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }


        public static DataTable QCGate_GetAlbagInfo(string procedure, string albagID)
        {
            DataTable dtab = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5))
                {
                    SqlCommand cmd = new SqlCommand(procedure, conn);
                    cmd.CommandType = CommandType.StoredProcedure; ;
                    cmd.Parameters.Add("@AlbagID", SqlDbType.VarChar).Value = albagID;

                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    conn.Open();
                    adp.Fill(dtab);

                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile(ex.ToString());
            }
            return dtab;
        }

        public static object QCGate_GetRunningInfoByAlbag(string albagID)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                SqlCommand cmd = new SqlCommand("sp_PMTT_QCGate_GetInfoRunningByAlbagID", conn);
                cmd.CommandType = CommandType.StoredProcedure; ;

                cmd.Parameters.Add("@AlbagID", SqlDbType.VarChar).Value = albagID;

                conn.Open();
                data_obj = cmd.ExecuteScalar();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }
        public static object QCGate_GetMachineRanByAlbag(string albagID)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                SqlCommand cmd = new SqlCommand("sp_PMTT_QCGate_GetMachineRanByAlbagID", conn);
                cmd.CommandType = CommandType.StoredProcedure; ;

                cmd.Parameters.Add("@AlbagID", SqlDbType.VarChar).Value = albagID;

                conn.Open();
                data_obj = cmd.ExecuteScalar();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }
        public static object QCGate_GetIndicationRanByAlbag(string albagID)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                SqlCommand cmd = new SqlCommand("sp_PMTT_QCGate_GetMachineRanByAlbagID", conn);
                cmd.CommandType = CommandType.StoredProcedure; ;

                cmd.Parameters.Add("@AlbagID", SqlDbType.VarChar).Value = albagID;

                conn.Open();
                data_obj = cmd.ExecuteScalar();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }

        public static object QCGate_GetAlbagIDRanByPanel(string panelID)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                SqlCommand cmd = new SqlCommand("sp_PMTT_QCGate_GetAlbagRanByPanelID", conn);
                cmd.CommandType = CommandType.StoredProcedure; ;

                cmd.Parameters.Add("@PanelID", SqlDbType.VarChar).Value = panelID;

                conn.Open();
                data_obj = cmd.ExecuteScalar();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }

        public static object QCGate_UpdateInfoRunning(string albagID, string indication, string machineID, string programname, string operatorID)
        {
            object data_obj = null;
            SqlConnection conn = new SqlConnection(connStringPMTT_TRACE_F5);
            try
            {
                SqlCommand cmd = new SqlCommand("sp_PMTT_QCGate_UpdateInfoRunning", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@AlbagID", SqlDbType.VarChar).Value = albagID;
                cmd.Parameters.Add("@Indication", SqlDbType.VarChar).Value = indication;
                cmd.Parameters.Add("@MachineID", SqlDbType.VarChar).Value = machineID;
                cmd.Parameters.Add("@ProgramName", SqlDbType.VarChar).Value = programname;
                cmd.Parameters.Add("@OperatorID", SqlDbType.VarChar).Value = operatorID;

                conn.Open();
                data_obj = cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }



        public static DataTable MANUALJig_GetEmapByEmapID(string panelID)
        {
            DataTable data_obj = new DataTable();
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                SqlCommand cmd = new SqlCommand("sms_EmapIdMergeDataStageAVIandQC", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@EmapID", SqlDbType.VarChar).Value = panelID;

                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                conn.Open();
                adp.Fill(data_obj);
                conn.Close();
            }
            catch (SqlException ex)
            {
                Global.WriteLogFile("[ExecuteScalar] - " + ex.ToString());
            }
            finally
            {
                conn.Dispose();
            }
            return data_obj;
        }
    }
}
