using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DAL;
using RwModule.Models;
using CommonModule.ViewModels;
using RwModule.ViewModels;
using DataObjects.Interfaces;
using DataObjects;

namespace RwModule.Helpers
{
    /// <summary>
    /// Вспомогательный класс, содержащий операции бизнес-логики, связанной с обработкой ЖД услуг.
    /// </summary>
    public class BusinessHelper
    {
        private WaitDlgViewModel wd;
        private IDbService repository;

        public BusinessHelper(IDbService _repository, WaitDlgViewModel _wd)
        {
            repository = _repository;
            wd = _wd;
        }

        public RwPlat AddRwPlat(RwPlat _model)
        {
            RwPlat res = null;
            if (_model != null)
            {
                using (var db = new RealContext())
                {
                    db.Entry(_model).State = System.Data.Entity.EntityState.Added;
                    if (db.SaveChanges() == 1)
                        res = _model;
                }
            }
            return res;
        }

        public bool SubmitSinksAction(RwPayActionViewModel[] _payactions, DateTime _datzakr)
        {
            if (_payactions == null || _payactions.Length == 0) return false;

            var actiontime = DateTime.Now;
            var userid = repository.UserToken;

            bool res = false;

            var rwlists = new List<int>();
            //var rwdocs = new List<long>();
            var rwplats = new List<int>();

            using (var db = new RealContext())
            {
                db.Configuration.AutoDetectChangesEnabled = true;
                using (var tran = db.Database.BeginTransaction())
                {
                    try
                    {
                        for (int i = 0; i < _payactions.Length; i++)
                        {
                            var pa = _payactions[i];
                            if (wd != null)
                                wd.Message = String.Format("Погашение {3}/{4}: \nОплата № {0} Документа № {1}  Сумма {2:#,##} ...", pa.NumPlat, pa.NumDoc, pa.Summa, i + 1, _payactions.Length);

                            var iddoc = pa.IdDoc;
                            var idlist = pa.IdRwList;
                            var idplat = pa.IdRwPlat;

                            var dbpa = new RwPaysArc
                            {
                                Iddoc = iddoc,
                                Idrwplat = idplat,
                                Payaction = pa.ActionType,
                                Summa = pa.Summa,
                                Datopl = _datzakr,
                                Userid = userid,
                                Atime = actiontime
                            };
                            db.RwPaysArcs.Add(dbpa);                            

                            if (idplat.HasValue && !rwplats.Contains(idplat.Value))
                                rwplats.Add(idplat.Value);
                            if (pa.ActionType == RwPayActionType.DoVozvrat || pa.ActionType == RwPayActionType.CloseVozvrat)
                            {
                                var idvozv = (int)iddoc;
                                if (iddoc > 0 && !rwplats.Contains(idvozv))
                                    rwplats.Add(idvozv);
                            }
                            else
                            {
                                if (idlist.HasValue && !rwlists.Contains(idlist.Value))
                                    rwlists.Add(idlist.Value);
                                if (iddoc.HasValue)// && !rwdocs.Contains(iddoc.Value))
                                //rwdocs.Add(iddoc.Value);
                                {
                                    var dbrwd = db.RwDocs.FirstOrDefault(d => d.Id_rwdoc == iddoc.Value);
                                    if (dbrwd == null) throw new Exception("Документ не найден в базе ID=" + iddoc.Value.ToString());
                                    dbrwd.Sum_opl += pa.Summa;
                                }
                            }
                            db.SaveChanges();

                            if (wd != null)
                                wd.Message += "Ok";
                        }

                        if (wd != null)
                            wd.Message = "Обновление итогов по перечням ...";

                        foreach (var idrwl in rwlists)
                        {
                            var dbrwl = db.RwLists.FirstOrDefault(l => l.Id_rwlist == idrwl);
                            if (dbrwl == null) throw new Exception("Перечень не найден в базе ID=" + idrwl.ToString());

                            var sumopl = db.RwDocs.Where(d => d.Id_rwlist == idrwl)
                                                  .SelectMany(d => db.RwPaysArcs.Where(r => r.Iddoc == d.Id_rwdoc && (r.Payaction == RwPayActionType.PayUsl || r.Payaction == RwPayActionType.CloseUsl)))
                                                  .Sum(r => (decimal?)r.Summa) ?? 0M;
                            var newPayStatus = Math.Abs(dbrwl.Sum_inv + dbrwl.Sum_invnds) == Math.Abs(sumopl) ? PayStatuses.TotallyPayed : (sumopl == 0 ? PayStatuses.Unpayed : PayStatuses.Payed);
                            dbrwl.Paystatus = newPayStatus;
                            dbrwl.Paydate = _datzakr;
                            dbrwl.Sum_opl = sumopl;
                            db.SaveChanges();
                        }

                        //if (wd != null)
                        //{
                        //    wd.Message += "Ок";
                        //    wd.Message += "\nОбновление документов ...";
                        //}

                        //foreach (var idrwd in rwdocs)
                        //{
                        //    var dbrwd = db.RwDocs.FirstOrDefault(d => d.Id_rwdoc == idrwd);
                        //    if (dbrwd == null) throw new Exception("Документ не найден в базе ID=" + idrwd.ToString());

                        //    var sumopl = db.RwPaysArcs.Where(r => r.Iddoc == idrwd && (r.Payaction == RwPayActionType.PayUsl || r.Payaction == RwPayActionType.CloseUsl))
                        //                              .Sum(r => (decimal?)r.Summa) ?? 0M;
                        //    dbrwd.Sum_opl = sumopl;
                        //    db.SaveChanges();
                        //}

                        if (wd != null)
                        {
                            wd.Message += "Ок";
                            wd.Message += "\nОбновление платёжек ...";
                        }

                        foreach (var idpl in rwplats)
                        {
                            var dbpl = db.RwPlats.FirstOrDefault(p => p.Idrwplat == idpl);
                            if (dbpl == null) throw new Exception("Платёжка не найдена в базе ID=" + idpl.ToString());

                            bool isvozv = dbpl.Direction == RwPlatDirection.In;
                            decimal sumopl = 0;
                            if (isvozv)
                                sumopl = db.RwPaysArcs.Where(r => r.Iddoc == idpl && (r.Payaction == RwPayActionType.CloseVozvrat || r.Payaction == RwPayActionType.DoVozvrat)).Sum(r => (decimal?)r.Summa) ?? 0M;
                            else
                                sumopl = db.RwPaysArcs.Where(r => r.Idrwplat == idpl && (r.Payaction == RwPayActionType.ClosePlat || r.Payaction == RwPayActionType.PayUsl)).Sum(r => (decimal?)r.Summa) ?? 0M;
                            dbpl.Ostatok = dbpl.Sumplat - sumopl;
                            if (dbpl.Ostatok == 0)
                                dbpl.Datzakr = _datzakr;
                            else
                                dbpl.Datzakr = null;
                            db.SaveChanges();
                        }

                        if (wd != null)
                        {
                            wd.Message += "Ок";
                            wd.Message += "\nПодтверждение всех операций ...";
                        }
                        
                        tran.Commit();

                        if (wd != null)
                            wd.Message += "Ок";

                        res = true;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                    return res;
                }
            }
        }

        public bool UndoSelPayActions(RwPayActionViewModel[] _payactions)
        {
            bool res = false;
            //var pacopy = _payactions.ToArray();// копируем, коллекцию для возможности её изменения при перечислении

            var rwlists = new List<int>();
            //var rwdocs = new List<long>();
            var rwplats = new List<int>();

            using (var db = new RealContext())
            {
                db.Configuration.AutoDetectChangesEnabled = true;
                using (var tran = db.Database.BeginTransaction())
                {
                    try
                    {
                        for (int i = 0; i < _payactions.Length; i++)
                        {
                            var pa = _payactions[i];
                            var parc = pa.PayArcInfo;
                            if (wd != null)
                                wd.Message = String.Format("Отмена погашения {3}/{4}: \nОплата № {0} Документа № {1}  Сумма {2:#,##} ...", pa.NumPlat, pa.NumDoc, pa.Summa, i + 1, _payactions.Length);

                            var iddoc = parc.Iddoc;
                            var idlist = pa.IdRwList;
                            var idplat = parc.Idrwplat;
                            var summa = pa.Summa;

                            db.Entry<RwPaysArc>(parc).State = System.Data.Entity.EntityState.Deleted;                           

                            if (idplat.HasValue && !rwplats.Contains(idplat.Value))
                                rwplats.Add(idplat.Value);
                            if (pa.ActionType == RwPayActionType.DoVozvrat || pa.ActionType == RwPayActionType.CloseVozvrat)
                            {
                                var idvozv = (int)iddoc;
                                if (iddoc > 0 && !rwplats.Contains(idvozv))
                                    rwplats.Add(idvozv);
                            }
                            else
                            {
                                if (idlist.HasValue && !rwlists.Contains(idlist.Value))
                                    rwlists.Add(idlist.Value);
                                if (iddoc.HasValue)
                                {
                                    var dbrwd = db.RwDocs.FirstOrDefault(d => d.Id_rwdoc == iddoc.Value);
                                    if (dbrwd == null) throw new Exception("Документ не найден в базе ID=" + iddoc.Value.ToString());
                                    dbrwd.Sum_opl -= pa.Summa;
                                }
                                    //rwdocs.Add(iddoc.Value);
                            }
                            db.SaveChanges();

                            if (wd != null)
                                wd.Message += "Ok";
                        }

                        foreach (var idrwl in rwlists)
                        {
                            var dbrwl = db.RwLists.FirstOrDefault(l => l.Id_rwlist == idrwl);
                            if (dbrwl == null) throw new Exception("Перечень не найден в базе ID=" + idrwl.ToString());

                            var sumopl = db.RwDocs.Where(d => d.Id_rwlist == idrwl)
                                                  .SelectMany(d => db.RwPaysArcs.Where(r => r.Iddoc == d.Id_rwdoc && (r.Payaction == RwPayActionType.PayUsl || r.Payaction == RwPayActionType.CloseUsl))
                                                  .Select(r => (decimal?)r.Summa)).Sum() ?? 0M;
                            var newPayStatus = Math.Abs(dbrwl.Sum_inv + dbrwl.Sum_invnds) == Math.Abs(sumopl) ? PayStatuses.TotallyPayed : (sumopl == 0 ? PayStatuses.Unpayed : PayStatuses.Payed);
                            dbrwl.Paystatus = newPayStatus;
                            if (newPayStatus == PayStatuses.Unpayed)
                                dbrwl.Paydate = null;
                            dbrwl.Sum_opl = sumopl;
                            db.SaveChanges();
                        }

                        //foreach (var idrwd in rwdocs)
                        //{
                        //    var dbrwd = db.RwDocs.FirstOrDefault(d => d.Id_rwdoc == idrwd);
                        //    if (dbrwd == null) throw new Exception("Документ не найден в базе ID=" + idrwd.ToString());

                        //    var sumopl = db.RwPaysArcs.Where(r => r.Iddoc == idrwd && (r.Payaction == RwPayActionType.PayUsl || r.Payaction == RwPayActionType.CloseUsl))
                        //                              .Sum(r => (decimal?)r.Summa) ?? 0M;
                        //    dbrwd.Sum_opl = sumopl;
                        //    db.SaveChanges();
                        //}

                        foreach (var idpl in rwplats)
                        {
                            var dbpl = db.RwPlats.FirstOrDefault(p => p.Idrwplat == idpl);
                            if (dbpl == null) throw new Exception("Платёжка не найдена в базе ID=" + idpl.ToString());

                            bool isvozv = dbpl.Direction == RwPlatDirection.In;

                            IQueryable<IGrouping<long?, RwPaysArc>> platpays = null;
                            if (isvozv)
                                platpays = db.RwPaysArcs.Where(r => r.Iddoc == idpl && (r.Payaction == RwPayActionType.CloseVozvrat || r.Payaction == RwPayActionType.DoVozvrat)).GroupBy(r => r.Iddoc);
                            else
                                platpays = db.RwPaysArcs.Where(r => r.Idrwplat == idpl && (r.Payaction == RwPayActionType.ClosePlat || r.Payaction == RwPayActionType.PayUsl)).GroupBy(r => (long?)r.Idrwplat);

                            var sum_dat = platpays.Select(g => new { Datopl = g.Max(i => i.Datopl), Sumopl = g.Sum(i => (decimal?)i.Summa) ?? 0M }).FirstOrDefault();
                            var sumopl = 0M;
                            DateTime? datzakr = null;
                            if (sum_dat != null)
                            {
                                sumopl = sum_dat.Sumopl;
                                datzakr = sum_dat.Datopl;
                            }
                            //var sumopl = platpays.Sum(r => (decimal?)r.Summa) ?? 0M;
                            dbpl.Ostatok = dbpl.Sumplat - sumopl;
                            if (dbpl.Ostatok == 0)
                                dbpl.Datzakr = datzakr;
                            else
                                dbpl.Datzakr = null;
                            db.SaveChanges();
                        }
                        tran.Commit();
                        res = true;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                    return res;
                }
            }            
        }
    }
}
