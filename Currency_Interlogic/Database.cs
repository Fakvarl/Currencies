﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using EntityFramework.BulkInsert.Extensions;
using System.Data.SqlClient;
using System.Data;
using System.Data.Entity;
using System.Threading;


namespace Currency_Interlogic
{
    public class Database
    {
        bool isReady = false;
        public currencyEntities DataBaseRef { get; set; }

        public bool IsReady
        {
            get { return isReady; }
        }
        static public DatabaseProxy Data { get; set; }

        protected List<currencies> currencies = new List<currencies>();
        protected List<Names> names = new List<Names>();

        public List<Names> Names
        {
            get { return names; }
            set { names = value; }
        }
        public List<currencies> Currencies
        {
            get { return currencies; }
            set { currencies = value; }
        }

        public Database()
        {
            DataBaseRef = new currencyEntities();
        }
        static Database()
        {
            if (Settings.SettingObj.ShouldLoad)
            {
                LoadFromXml(Settings.SettingObj.XmlAdress);
            }
        }
        public void LoadFromBase(IProgress<int> progress)
        {

            currencyEntities newData = new currencyEntities();

            List<Names> tempNames = newData.Names.ToList();

            lock (names)
            {
                Names = tempNames;
            }
            progress.Report(10);
            List<currencies> currenciesTemp = new List<currencies>();
            int maxNumber = newData.currencies.Count(), index = 0, step = maxNumber / 90, next = step ;
            foreach(var el in newData.currencies)
            {
                currenciesTemp.Add(el);
                ++index;
                if(index >= next)
                {
                    next += step;
                    progress.Report(10 + index * 90 / maxNumber);
                    
                }
            }
            
            lock (currencies)
            {
                Currencies = currenciesTemp;
            }


            isReady = true;


        }



        static public void LoadFromXml(string path)
        {
            currencyEntities DataBaseRef = new currencyEntities();
            var doc = XDocument.Load(path);
            CurrencyDatabase dat = new CurrencyDatabase();

            HashSet<string> names = new HashSet<string>();

            foreach (var el in doc.Descendants("Rate"))
            {
                if (el.Element("Currency") != null && el.Element("Currency").Value != "")
                {
                    names.Add(el.Element("Currency").Value);

                }
            }

            var i = (DataBaseRef.Names.Select(b => b.ID)).ToArray().LastOrDefault() + 1;

            foreach (var el in names)
            {
                if (el != null)
                    dat.addRecord(new Currency(el) { ID = i });
                ++i;
            }


            i = 0;

            foreach (var el in doc.Descendants("Rate"))
            {
                if (el.Element("Currency") != null && (el.Ancestors("Rates") != null && el.Ancestors("Rates").First().Element("DateTime") != null)
                    && (el.Element("Change") != null) && el.Element("ChangePercent") != null
                    && el.Element("IsChangeDown") != null && el.Element("Value") != null && el.Element("Currency").Value != "")
                {
                    var comp = el.Element("Currency").Value;
                    XElement el2 = (el.Ancestors("Rates")).First().Element("DateTime");
                    Currency c = new Currency(comp);
                    c.AddComponent(new CurrencyData
                    {
                        Change = float.Parse(el.Element("Change").Value, System.Globalization.CultureInfo.InvariantCulture),
                        ChangePercent = float.Parse(el.Element("ChangePercent").Value, System.Globalization.CultureInfo.InvariantCulture),
                        Date = Convert.ToDateTime(el2.Value),
                        IsChangeDown = Convert.ToInt32(Convert.ToBoolean(el.Element("IsChangeDown").Value)),
                        Value = float.Parse(el.Element("Value").Value, System.Globalization.CultureInfo.InvariantCulture)
                    });
                    dat.addRecord(c);
                    i++;
                    if (i == -1)
                        break;
                }
            }

            List<currencies> check1 = new List<currencies>();
            List<Names> check2 = new List<Names>();
            dat.CopyTo(check1, check2);

           
        
            DataBaseRef.BulkInsert(check2);
            DataBaseRef.BulkInsert(check1);

        }


    }
}
