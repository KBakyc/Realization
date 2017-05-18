using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using RwModule.Models;
using DataObjects;
using DataObjects.Interfaces;
using System.ComponentModel;
using DAL;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель отображения погашения ЖД услуги на платёж.
    /// </summary>
    public class RwPayActionViewModel : BasicNotifier
    {
        public RwPayActionViewModel(RwPayActionType _actionType)
        {
            actionType = _actionType;
        }
        
        private RwPayActionViewModel(RwPaysArc _pa)
        {
            payArcInfo = _pa;
            actionType = _pa.Payaction;
            IdRwPlat = _pa.Idrwplat;
            IdDoc = _pa.Iddoc;
            Summa = _pa.Summa;
            Notes = _pa.Notes;
            ActionTime = _pa.Atime;            
        }

        public static RwPayActionViewModel FromRwPaysArc(RwPaysArc _pa, RealContext _db)
        {
            var res = new RwPayActionViewModel(_pa);
            if (_pa.Idrwplat > 0)
            {
                var plat = _db.RwPlats.FirstOrDefault(p => p.Idrwplat == _pa.Idrwplat);
                if (plat != null)
                {
                    res.NumPlat = plat.Numplat.ToString();
                    res.DatPlat = plat.Datplat;
                }
            }
            if (_pa.Iddoc > 0)
            {
                if (_pa.Payaction == RwPayActionType.DoVozvrat || _pa.Payaction == RwPayActionType.CloseVozvrat)
                {
                    var vozv = _db.RwPlats.FirstOrDefault(p => p.Idrwplat == _pa.Iddoc);
                    if (vozv != null)
                    {
                        res.NumDoc = vozv.Numplat.ToString();
                        res.DatDoc = vozv.Datplat;
                    }
                }
                else
                {
                    var rwdoc = _db.RwDocs.FirstOrDefault(d => d.Id_rwdoc == _pa.Iddoc);
                    if (rwdoc != null)
                    {
                        res.NumDoc = rwdoc.Num_doc;
                        res.DatDoc = rwdoc.Dat_doc;
                        res.IdRwList = rwdoc.Id_rwlist;
                        var paytype = _db.GetRwPayType(rwdoc.Paycode);
                        if (paytype != null)
                            res.Notes = String.Format("({0}) {1}", paytype.Paycode, paytype.Payname);
                    }
                }
            }
            return res;
        }

        private RwPaysArc payArcInfo;
        public RwPaysArc PayArcInfo
        {
            get { return payArcInfo; }
        }

        private RwPayActionType actionType;
        public RwPayActionType ActionType
        {
            get { return actionType; }
        }

        public int? IdRwPlat { get; set; }       
        public long? IdDoc  { get; set; }
        public int? IdRwList  { get; set; }

        public decimal Summa  { get; set; }
        public string Notes { get; set; }


        public string NumPlat { get; set; }
        public DateTime? DatPlat { get; set; }
        public string NumDoc { get; set; }
        public DateTime? DatDoc { get; set; }

        public DateTime ActionTime { get; set; }
    }
}
