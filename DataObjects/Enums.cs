using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace DataObjects
{
    //public enum SfViewForm { Default, ForBank}

    /// <summary>
    /// Вид погашения предоплаты (Счёт-фактура, Возврат)
    /// </summary>
    public enum PayActionTypes {Sf, Vozvrat, Penalty}

    /// <summary>
    /// Режим загрузки (с параметрами или с готовыми данными)
    /// </summary>
    public enum LoadMode { ByParams, ByData }

    /// <summary>
    /// Тип строчки (Продуктовая, доп. платеж, итог...)
    /// </summary>
    public enum LineTypes
    {
        Product, DopPay, DopItog, SfItog
    }

    //public enum DocTypes
    //{
    //    [Description("Неизвестно")]
    //    Unknown = 0,
    //    [Description("Накладная")]
    //    Rnn = 1,
    //    [Description("ТН2")]
    //    Tn2 = 2
    //}

    public enum PayDocTypes
    {
        Sf = 0,
        Penalty = 1
    }


    public enum NdsTypes
    {
        Unknown = 0,
        Product = 1,
        Provoz = 2,
        Akciz = 3,
        Obchepit = 4,
        UslugiSvjazi = 5
    }

    /// <summary>
    /// Статусы счёта-фактуры
    /// </summary>
    public enum LifetimeStatuses
    {
        Unknown = 0,
        Created = 1,
        Accepted = 2,
        Edited = 4,
        Deleted = 5,
        Purged = 6
            //,
        //EsfnCreated = 11
        //    ,
        //Payed = 7,
        //TotallyPayed = 8,
        //PaysCancelled = 9
    }
    
    public enum PayStatuses : byte
    {
        Unpayed = 0,
        Payed = 1,
        TotallyPayed = 2
    }
    
    public enum PayActions
    {
        Payment = 8,
        UndoPays = 9
    }

    /// <summary>
    /// Статусы предоплаты
    /// </summary>
    public enum PredoplStatuses
    {
        Payed = 11,
        UndoPays = 13
    }

    public enum SelectTypes
    { 
        All = 1,
        Selected = 2
    }

    public enum TrackingInfo
    {
        Unchanged,
        Created,
        Updated,
        Deleted
    }

    public enum ReportParameters
    { 
        None = 0,
        PoupDateDate = 1,
        PoupDateDateKpok = 2,
        PoupDateDateKpok_Alt = 3,
        Generic = -1
    }

    public enum ReportModes
    { 
        Server = 0,
        Local = 1
    }

    public enum PageSize
    { 
        A4, A3
    }

    public enum SyncStatuses
    {
        Ok, Busy, Error
    }

    public enum ApplyFeature
    {
        Yes, No, Ask
    }

    public enum DebtTypes
    {
        Debet, Credit
    }

    public enum InOtgrTypes
    {
        RawMaterials,
        InEmptyWagons,
        OutEmptyWagons
    }

    public enum Directions
    {
        Unknown,
        In,
        Out
    }

    public enum JournalKind
    {
        Sell,
        Buy
    }

    //public enum ChangeOtgrModes
    //{
    //    SingleLine = 1,
    //    AllDocLines = 2,

    //}

    public enum TransportTypes
    {
        None = 0,
        SelfAuto = 1, //Самовывоз
        Pipe = 2, //Трубопровод
        Railway = 3, //Железная дорога
        Tanks = 4, //Передача в резервуарах
        SeaTransport = 5, //Морским транспортом
        Auto = 6, //Вывоз автотранспортом
        CenterAuto = 7 //Центровывоз
    }

    public enum RefundTypes : byte
    {
        [Description("Любые")]
        Any = 0,
        [Description("Возмещаемые")]
        Refund = 1,
        [Description("Невозмещаемые")]
        NoRefund = 2
    }
}