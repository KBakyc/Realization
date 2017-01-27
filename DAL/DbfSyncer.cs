using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;
using DataObjects;
using DataObjects.Interfaces;
using System.Xml.Linq;
using DataObjects.Helpers;
using System.IO;

namespace DAL
{
    public enum SyncAction{Insert, Update}
    
    public class DbfSyncer
    {
        //private string path_744 = String.IsNullOrWhiteSpace(Properties.Settings.Default.Dbf_Path_F744) ? Environment.CurrentDirectory : Properties.Settings.Default.Dbf_Path_F744;
        //private string path_FDebCred = String.IsNullOrWhiteSpace(Properties.Settings.Default.Dbf_Path_FDebCred) ? Environment.CurrentDirectory : Properties.Settings.Default.Dbf_Path_FDebCred;        
        private IDbService repository;

        private string[] CONN_INIT = { "SET NULL OFF", "SET REPROCESS TO 120 SECONDS", "SET DELETED OFF" };
        private Dictionary<OleDbConnection, List<String>> lockedTables = new Dictionary<OleDbConnection, List<string>>();

        public DbfSyncer(IDbService _rep)
        {
            if (_rep == null) throw new ArgumentNullException("Repository");
            repository = _rep;
        }        
        
        public string LastError { get; set; }

        private bool DoExit(string _err)
        {
            if (!String.IsNullOrEmpty(_err))
                LastError = _err;
            return String.IsNullOrEmpty(LastError);
        }

        private void Unlock(OleDbConnection _conn, string _tblname)
        {
            string ctxt;
            if (!Used(_conn, _tblname))            
                ctxt = "EXECS(" +
                       "[USE " + _tblname + " SHARED] + chr(13) + " +
                       "[UNLOCK in " + _tblname + "])";
            else
                ctxt = "EXECS(" +
                       "[UNLOCK in " + _tblname + "])";
            var cmd = _conn.CreateCommand();
            cmd.CommandText = ctxt;
            cmd.ExecuteNonQuery();
        }
        
        private bool Used(OleDbConnection _conn, string _table)
        {
            bool res = false;
            string ctxt = string.Format("USED('{0}')", _table);
            var cmd = new OleDbCommand(ctxt, _conn);
            try
            {
                var rUsed = cmd.ExecuteScalar();
                res = Convert.ToBoolean(rUsed);
            }
            catch
            {
                return false;
            }
            return res;
        }

        private bool LockRecords(OleDbConnection _conn, string _table, string _where, params OleDbParameter[] _par)
        {
            bool res = true;
            if (_conn != null && !String.IsNullOrEmpty(_table) && !String.IsNullOrEmpty(_where))
            {
                OleDbCommand cmd = _conn.CreateCommand();
                cmd.CommandText = String.Format("SELECT RECNO() AS rn FROM {0} WHERE {1}", _table, _where);
                if (_par != null && _par.Length > 0)
                    cmd.Parameters.AddRange(_par);
                List<int> rns = new List<int>();
                try
                {
                    if (_conn.State != System.Data.ConnectionState.Open)
                        _conn.Open();

                    OleDbDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        int rn = Convert.ToInt32(reader["rn"]);
                        rns.Add(rn);
                    }
                    reader.Close();
                }
                catch
                {
                    res = false;
                }

                cmd.Parameters.Clear();

                if (res && rns.Count > 0)
                {
                    string rnstr = String.Join(",", rns.Select(i => i.ToString()).ToArray());
                    string ctxt = null;
                    if (!Used(_conn, _table))
                        ctxt = "EXECS(" +
                               "[USE " + _table + " SHARED] + chr(13) + " +
                               "[RETURN RLOCK('" + rnstr + "','" + _table + "')])";
                    else
                        ctxt = String.Format("RLOCK('{0}','{1}')", rnstr, _table);

                    cmd.CommandText = ctxt;
                    try
                    {
                        var rLock = cmd.ExecuteScalar();
                        res = Convert.ToBoolean(rLock);
                    }
                    catch
                    {
                        res = false;
                    }
                }
            
            }
            return res;
        }

        private bool LockTable(OleDbConnection _conn, string _table)
        {
            bool res = false;

            if (lockedTables.ContainsKey(_conn) && lockedTables[_conn].Contains(_table))
                return true;

            if (String.IsNullOrEmpty(_table) || _conn == null)
                return false;

            string ctxt = String.Format("FLOCK('{0}')",_table);

            if (!Used(_conn, _table))
                ctxt = "EXECS(" +
                       "[USE " + _table + " SHARED] + chr(13) + " +
                       "[RETURN FLOCK('" + _table + "')])";

            var cmd = new OleDbCommand(ctxt, _conn);
            if (_conn.State != System.Data.ConnectionState.Open)
                _conn.Open();

            try
            {
                var rLock = cmd.ExecuteScalar();
                res = Convert.ToBoolean(rLock);
            }
            catch
            {
                return false;
            }

            if (res)
            {
                if (!lockedTables.ContainsKey(_conn))
                    lockedTables[_conn] = new List<string>();
                lockedTables[_conn].Add(_table);
            }
            return res;
        }

        private bool Exists(OleDbConnection _conn, string _table, string _where, params OleDbParameter[] _pars)
        {
            bool res = false;
            if (String.IsNullOrEmpty(_table) || _conn == null)
                return false;
            if (String.IsNullOrEmpty(_where))
                _where = ".T.";
            string ctxt = null;
            if (!Used(_conn, _table))
                ctxt = "EXECS(" +
                       "[USE " + _table + " SHARED] + chr(13) + " +
                       "[LOCATE FOR " + _where + "] + chr(13) + " +
                       "[RETURN FOUND('" + _table + "')])";
            else
                ctxt = "EXECS(" +
                       "[SELECT " + _table + "] + chr(13) + " +
                       "[LOCATE FOR " + _where + "] + chr(13) + " +
                       "[RETURN FOUND('" + _table + "')])";
            var cmd = new OleDbCommand(ctxt, _conn);
            
            if (_pars != null && _pars.Length > 0)
                cmd.Parameters.AddRange(_pars);

            if (_conn.State != System.Data.ConnectionState.Open)
                _conn.Open();

            try
            {
                var rFound = cmd.ExecuteScalar();
                res = Convert.ToBoolean(rFound);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }

            return res;
        }

        private bool UpdateTableAction(OleDbConnection _conn, string _tname, string _set, string _wh, params OleDbParameter[] _pars)
        {
            if (_conn == null || String.IsNullOrEmpty(_tname) || String.IsNullOrEmpty(_wh) || String.IsNullOrEmpty(_set)) return false;

            OleDbCommand cmd = _conn.CreateCommand();
            cmd.CommandText = String.Format("UPDATE {0} SET {1} WHERE {2}", _tname, _set, _wh);
            if (_pars != null && _pars.Length > 0)
                cmd.Parameters.AddRange(_pars);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                return DoExit(ex.Message);
            }

            return true;
        }

        private bool DeleteFromTableAction(OleDbConnection _conn, string _tname, string _wh, params OleDbParameter[] _pars)
        {
            if (_conn == null || String.IsNullOrEmpty(_tname) || String.IsNullOrEmpty(_wh)) return false;

            OleDbCommand cmd = _conn.CreateCommand();
            cmd.CommandText = String.Format("DELETE FROM {0} WHERE {1}", _tname, _wh);
            if (_pars != null && _pars.Length > 0)
                cmd.Parameters.AddRange(_pars);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                return DoExit(ex.Message);
            }

            return true;
        }

// работа с конкретными DBF

        //public bool F744Exists(DateTime _fdate)
        //{                        
        //    string shortpath = MakeF744ShortPath(_fdate);
        //    string fpath = Path.Combine(path_744, shortpath + "P744A.XML");
        //    return File.Exists(fpath);
        //}

        //private string MakeF744ShortPath(DateTime _fdate)
        //{
        //    var year = _fdate.Year;
        //    var month = _fdate.Month;
        //    var day = _fdate.Day;
        //    return String.Format(@"{1:00}_{0}\D{2:00}\", year, month, day);
        //}

        //private bool CopyOldF744(DateTime _oldfdate, string _newshortpath)
        //{
        //    string oldshortpath = MakeF744ShortPath(_oldfdate);
        //    string oldfpath = Path.Combine(path_744, oldshortpath);
        //    string newpath = Path.Combine(path_744, _newshortpath);
        //    bool res = true;
        //    try
        //    {
        //        File.Copy(oldfpath + "P744A.DBF", newpath + "P744A.DBF", true);
        //        if (File.Exists(oldfpath + "P744A.CDX"))
        //            File.Copy(oldfpath + "P744A.CDX", newpath + "P744A.CDX", true);
        //    }
        //    catch
        //    {
        //        res = false;
        //    }
        //    return res;
        //}

        //public bool SaveF744ToDBF(DateTime _fdate)
        //{
        //    bool res = true;
        //    string shortpath = MakeF744ShortPath(_fdate);
        //    string fullpath = Path.Combine(path_744, shortpath);
        //    string fpath = fullpath + "P744A.DBF";
        //    if (!Directory.Exists(fullpath))
        //        Directory.CreateDirectory(fullpath);
        //    if (File.Exists(fpath))
        //    {
        //        File.Delete(fpath);
        //        File.Delete(fullpath + "P744A.CDX");
        //    }            
        //    res = CopyOldF744(_fdate.AddDays(-7), shortpath);
        //    if (!res)
        //        for (int i = -8; !res && i > -365; i--)
        //            res = CopyOldF744(_fdate.AddDays(i), shortpath);

        //    if (res)
        //        using (DbfHelper dh = new DbfHelper(fullpath))
        //        {
        //            var conn = dh.InitConnection("SET NULL OFF", "SET REPROCESS TO 30 SECONDS", "SET DELETED ON");
        //            if (conn == null) return false;

        //            res = InitF744dAction(conn, _fdate);
        //            if (!res) return false; ;

        //            arc_f744[] f744s = null;

        //            using (var dc = new DbfDCDataContext())
        //            {
        //                f744s = dc.arc_f744s.Where(f => f.data == _fdate).ToArray();
        //            }

        //            for (int i = 0; i < f744s.Length; i++)
        //            {
        //                res = AddF744RecordAction(conn, f744s[i]);
        //                if (!res) break;
        //            }
        //        }

        //    return res;
        //}

        //public bool SaveF744ToXML(DateTime _fdate)
        //{
        //    bool res = true;
        //    string shortpath = MakeF744ShortPath(_fdate);
        //    string fullpath = Path.Combine(path_744, shortpath);
        //    string fpath = fullpath + "P744A.XML";
        //    if (!Directory.Exists(fullpath))
        //        Directory.CreateDirectory(fullpath);
        //    if (File.Exists(fpath))
        //        File.Delete(fpath);

        //    arc_f744[] f744s = null;

        //    using (var dc = new DbfDCDataContext())
        //        f744s = dc.arc_f744s.Where(f => f.data == _fdate).ToArray();

        //    XDocument x744 = InitF744XMLAction(_fdate);
        //    var root = x744.Root.Elements("ReportHeader").First();

        //    for (int i = 0; i < f744s.Length; i++)
        //    {
        //        res = AddF744ToXMLRecordAction(root, f744s[i]);
        //        if (!res) break;
        //    }

        //    try
        //    {
        //        x744.Save(fpath);
        //        res = true;
        //    }
        //    catch
        //    {
        //        res = false;
        //    }

        //    return res;
        //}

        //private XDocument InitF744XMLAction(DateTime _fdate)
        //{
        //    XDocument doc = new XDocument(new XDeclaration("1.0", null, null), null);
        //    //doc.Add(new XDeclaration("1.0", null, null));
        //    doc.Add(
        //        new XElement("M744", new XAttribute("Create", DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")),
        //                             new XElement("ReportHeader", new XAttribute("FrameId","744"),
        //                                                            new XAttribute("ReporterCode","5778477"),
        //                                                            new XAttribute("ReporterName","ОАО \"НАФТАН\""),
        //                                                            new XAttribute("PeriodEnd",_fdate.ToString("yyyyMMdd")),
        //                                                            new XAttribute("CheckStateInt",0),
        //                                                            new XAttribute("CheckState","NoChecked")
        //                                           )
        //                   )
        //        );

        //    return doc;            
        //}

        //private bool AddF744ToXMLRecordAction(XElement _root, arc_f744 _frec)
        //{
        //    XElement section = new XElement(String.Format("section{0}", _frec.section));
        //    section.Add
        //        (
        //        new XElement("indicator_id", _frec.indicator_id),
        //        new XElement("code", _frec.nom_stroki),
        //        new XElement("caption", ""),
        //        new XElement("parent_code"),
        //        new XElement("section", _frec.section),
        //        new XElement("strong_sum", true),
        //        new XElement("payable", _frec.g01r1744),
        //        new XElement("overdue", _frec.g02r1744),
        //        new XElement("parents", 0)
        //        );

        //    _root.Add(section);

        //    return true;
        //}

        //private bool InitF744dAction(OleDbConnection _conn, DateTime _fdate)
        //{
        //    OleDbCommand cmd = _conn.CreateCommand();
        //    cmd.CommandText = @"UPDATE P744A SET g01r1744=0, g02r1744=0, g03r1744=0, data=?";
        //    cmd.Parameters.Add(new OleDbParameter("data", _fdate) { DbType = System.Data.DbType.Date });
        //    try
        //    {
        //        cmd.ExecuteNonQuery();
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        //private bool AddF744RecordAction(OleDbConnection _conn, arc_f744 _frec)
        //{
        //    OleDbCommand cmd = _conn.CreateCommand();

        //    cmd.CommandText = @"UPDATE P744A SET g01r1744=?, g02r1744=?, g03r1744=? WHERE nom_stroki=?";

        //    cmd.Parameters.Clear();
        //    cmd.Parameters.AddRange(new OleDbParameter[]{
        //                            new OleDbParameter("g01r1744", _frec.g01r1744){DbType = System.Data.DbType.Decimal},
        //                            new OleDbParameter("g02r1744", _frec.g02r1744){DbType = System.Data.DbType.Decimal},
        //                            new OleDbParameter("g03r1744", _frec.g03r1744){DbType = System.Data.DbType.Decimal},
        //                            new OleDbParameter("nom_stroki", _frec.nom_stroki){DbType = System.Data.DbType.AnsiString}, 
        //    });

        //    try
        //    {
        //        cmd.ExecuteNonQuery();
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return true;
        //}             

    }
}
