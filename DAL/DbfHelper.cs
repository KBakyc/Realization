using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;

namespace DAL
{
    /// <summary>
    /// Вспомогательный класс для работы с DBF
    /// </summary>
    public class DbfHelper : IDisposable
    {
        private const String dataProvider = @"vfpoledb.1"; // Провайдер

        private String dataPath;

        private OleDbConnection conn;

        public DbfHelper(string _path)
        {
            dataPath = _path;
        }

        public OleDbConnection InitConnection(params string[] _instr)
        { 
            var conn = Connection;
            if (conn != null)
            {
                conn.Open();
                if (_instr != null && _instr.Length > 0)
                {
                    var cmd = conn.CreateCommand();
                    for (int i = 0; i < _instr.Length; i++)
                    {
                        cmd.CommandText = _instr[i];
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return conn;
        }

        //private OleDbCommand command;
        //public OleDbCommand Command
        //{
        //    get
        //    {
        //        if (Connection == null || Connection.State != System.Data.ConnectionState.Open)
        //            command = null;
        //        else
        //            if (command == null)
        //                command = new OleDbCommand() { Connection = Connection };
        //        return command;
        //    }
        //}

        public String ConnString
        {
            get 
            { 
                string str = null;
                if (!String.IsNullOrEmpty(dataPath))
                {
                    OleDbConnectionStringBuilder bldr = new OleDbConnectionStringBuilder();
                    bldr.DataSource = dataPath; // Указываем путь
                    bldr.Provider = dataProvider; // Указываем провайдера
                    bldr.OleDbServices = -8;
//All services (default) "OLE DB Services = -1;"
//All except pooling and automatic transaction enlistment "OLE DB Services = -4;"
//All except Client Cursor Engine "OLE DB Services = -5;"
//All except pooling, automatic transaction enlistment, and Client Cursor Engine "OLE DB Services = -8;"
//Pooling and automatic transaction enlistment only, session level aggregation only "OLE DB Services = 3;"
//No services "OLE DB Services = 0;"
                    str = bldr.ConnectionString;
                }
                return str; 
            }
        }

        public OleDbConnection Connection
        {
            get
            {
                if (conn == null && !String.IsNullOrEmpty(dataPath))
                    conn = new OleDbConnection(ConnString);                
                return conn;
            }
        }

        public void ExecuteNonQuery(string _cmdtext)
        {
            if (Connection != null && !String.IsNullOrEmpty(_cmdtext))
            {
                var istate = Connection.State;
                if (istate == System.Data.ConnectionState.Closed)
                    Connection.Open();

                OleDbCommand cmd = Connection.CreateCommand();
                cmd.CommandText = _cmdtext; // Задаем оператор SQL
                cmd.ExecuteNonQuery();

                if (istate == System.Data.ConnectionState.Closed)
                    Connection.Close();
            }
        }

        public void ExecuteNonQuery(OleDbCommand _cmd)
        {
            if (Connection != null && _cmd != null && !String.IsNullOrEmpty(_cmd.CommandText))
            {
                var istate = Connection.State;
                if (istate == System.Data.ConnectionState.Closed)
                    Connection.Open();
                
                _cmd.Connection = Connection;
                _cmd.ExecuteNonQuery();

                if (istate == System.Data.ConnectionState.Closed)
                    Connection.Close();
            }
        }

        public void CreateTableDBF(string _tname, string _columns)
        {
            CreateTableDBF(null, _tname, _columns);
        }

        public void CreateTableDBF(string _path, string _tname, string _columns)
        {
            string fullpath = String.IsNullOrEmpty(_path) ? dataPath : System.IO.Path.Combine(dataPath, _path);
            string tmpFileName = "tmp12345.dbf";
            string fullTmpFileName = String.Format(@"{0}\{1}", fullpath, tmpFileName);
            string fullTableName = String.Format(@"{0}\{1}.dbf", fullpath, _tname);

            if (System.IO.File.Exists(fullTmpFileName))
                System.IO.File.Delete(fullTmpFileName);
            if (System.IO.File.Exists(fullTableName))
                System.IO.File.Delete(fullTableName);

            string cmdstr = String.Format(@"CREATE TABLE {0} ({1})", fullTmpFileName, _columns);
            ExecuteNonQuery(cmdstr);
            cmdstr = "EXECS(" +
                              @"[USE " + fullTmpFileName + "] + chr(13) + " +
                              @"[COPY TO " + fullTableName + " TYPE FOX2X AS 866] + chr(13) + " + 
                              @"[CLOSE DATA])";
            ExecuteNonQuery(cmdstr);

            if (System.IO.File.Exists(fullTmpFileName))
                System.IO.File.Delete(fullTmpFileName);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (conn != null)
            {
                conn.Close();
                conn.Dispose();
                conn = null;
            }
        }

        #endregion
    }
}
