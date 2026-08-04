using System;
using System.Collections.Generic;
//using System.Linq;
using System.Threading;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DefaultNS;
using System.Text;

static class Global
{
    public static string PassworDefaut = "19042006";
    public static string VersionSoftware = "1.0.1";
    public static string[] ProductHistory = {"Ver 1.0.0 - Date: 25/11/2024 - Ban hành lần đầu","Ver 1.0.1 - Date: 07/01/2025 - Theem function check scan handy"
                                                };
    static public string ApplicationFullPathName() { return System.Reflection.Assembly.GetExecutingAssembly().Location; }
    static public string ApplicationPath() { return System.IO.Path.GetDirectoryName(ApplicationFullPathName()); }





    //static public void LoadComboboxFromDatatable(ComboBox cbo, DataTable data_table)
    //{
    //    cbo.Items.Clear();
    //    if (data_table != null)
    //    {
    //        //for (int i = 0; i < data_table.Rows.Count - 1; i++)
    //        //{
    //        //    cbo.Items.Add(data_table.Columns[0].);   
    //        //}
    //        foreach (DataRow row in data_table.Rows)
    //        {
    //            cbo.Items.Add(row.ToString()); 
    //        }
    //    }
    //}

    //static public void LoadComboboxFromDatatable(ToolStripComboBox cbo, DataTable data_table)
    //{
    //    cbo.Items.Clear();
    //    if (data_table != null)
    //    {
    //        //for (int i = 0; i < data_table.Rows.Count - 1; i++)
    //        //{
    //        //    cbo.Items.Add(data_table.Columns[0].);   
    //        //}
    //        foreach (DataRow row in data_table.Rows)
    //        {
    //            cbo.Items.Add(row.ToString());
    //        }
    //    }
    //}

    public static void ReleaseObject(object obj)
    {
        try
        {
            System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
            obj = null;
        }
        catch
        {
            obj = null;
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
    public static void WriteLog(string message)
    {
        try
        {
            var _logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogSystem");
            // Tạo thư mục nếu chưa có
            if (!Directory.Exists(_logFolder))
                Directory.CreateDirectory(_logFolder);

            // Tên file theo ngày
            string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            string filePath = Path.Combine(_logFolder, fileName);

            // Nội dung log
            string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}";

            File.AppendAllText(filePath, logLine + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // Có thể optionally xử lý lỗi ghi log ở đây hoặc bỏ qua để tránh crash
        }
    }



    static public void WriteLogFile(string stext)
    {
        string MyPath = ApplicationPath() + @"\EventsLog\";
        try
        {
            string appendText = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff] - ") + stext + "\r\n";
            if (!Directory.Exists(MyPath)) Directory.CreateDirectory(MyPath);
            MyPath += DateTime.Now.ToString("yyyyMMdd_HH") + "[" + Process.GetCurrentProcess().ProcessName + "].log";
            if (File.Exists(MyPath) == false)
            {
                // Create a file to write to.
                string createText = "Đây là Log file phần mềm Albag Combine \r\n";
                File.WriteAllText(MyPath, createText);
            }
            File.AppendAllText(MyPath, appendText);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }
}
