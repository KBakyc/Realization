using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DataObjects.Collections;

namespace DataObjects
{
    [Serializable]
    public class OtgrLine: ITrackable
    {
        public OtgrLine()
        {
        }

        public OtgrLine(int _idp623)
        {
            idp623 = _idp623;
        }
        
        public int? IdInvoiceType { get; set; }
        public string DocumentNumber { get; set; }
        public string Series { get; set; }
                             
        public int Kdog { get; set; }
        public string WL_S { get; set; }
        public string KodDav { get; set; }
        public short Kstr { get; set; }
        public short IdSpackage { get; set; }
        public short? IdSpurpose { get; set; }
        public int KodRaznar { get; set; }
        public int Maker { get; set; }
        public int IdProdcen { get; set; }
        public int? IdAct { get; set; }
        public int IdVozv { get; set; }
        public short PrVzaim { get; set; }
        public short SourceId { get; set; }
        public int Period { get; set; }
        public string Gnprc { get; set; }
        public string Marshrut { get; set; }
        public bool Bought { get; set; }

        private bool isChecked;
        
        private int idp623;
        public int Idp623
        {
            get { return idp623; }
            set { idp623 = value; }
        }

        public int Idrnn { get; set; }

        public int Kpok { get; set; }
        public int Kgr { get; set; }

        public short Kodf { get; set; }
        public int Poup { get; set; }
        public short Pkod { get; set; }

        public DateTime Datgr { get; set; }
        public DateTime Datnakl { get; set; }
        public int Kpr { get; set; }
        public decimal Kolf { get; set; }
        public decimal Cena { get; set; }
        public int Vidcen { get; set; }
        public string Kodcen { get; set; }
        public decimal Prodnds { get; set; }
        public decimal? SumNds { get; set; }
       
        public bool PrSv { get; set; } // признак собственного вагона
        public short Provoz { get; set; }

        public string Nomavt { get; set; }
        public string Ndov { get; set; }
        public string Fdov { get; set; }
        public DateTime? DatDov { get; set; }

        public string RwBillNumber { get; set; }
        public int Nv { get; set; }
        public int Stgr { get; set; }
        public int Stotpr { get; set; }
        public int Stn_per { get; set; }

        public DateTime? Dataccept { get; set; }
        public DateTime? Datarrival { get; set; }
        public DateTime? Datdrain { get; set; }
        public DateTime? DeliveryDate { get; set; }
        
        public decimal Sper { get; set; }
        public decimal Nds { get; set; }
        public decimal Ndssper { get; set; }
        public decimal Dopusl { get; set; }
        public decimal Ndst_dop { get; set; }
        public decimal Ndsdopusl { get; set; }

        public short TransportId { get; set; }

        public int VidAkc { get; set; }
        public decimal AkcStake { get; set; }
        public string AkcKodVal { get; set; }
        public int? MeasureUnitId { get; set; }
        public decimal Density { get; set; }
        public DateTime? DatKurs { get; set; }
        


        public bool IsChecked { get; set; }

        public string[] StatusMsgs { get; set; }
        public short StatusType { get; set; }

        #region ITrackable Members

        public TrackingInfo TrackingState { get; set; }

        #endregion
    }
}
