﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTAPI_Sample_Console_TimeAndSales
{
    using TradingTechnologies.TTAPI;

    /// <summary>
    /// Main TT API class
    /// </summary>
    class TTAPIFunctions : IDisposable
    {
        /// <summary>
        /// Declare the TTAPI objects
        /// </summary>
        private UniversalLoginTTAPI m_apiInstance = null;
        private WorkerDispatcher m_disp = null;
        private bool m_disposed = false;
        private InstrumentLookupSubscription m_req = null;
        private TimeAndSalesSubscription m_ts = null;


        /// <summary>
        /// Default constructor
        /// </summary>
        public TTAPIFunctions()
        {
        }

        /// <summary>
        /// Create and start the Dispatcher
        /// </summary>
        public void Start()
        {
            // Attach a WorkerDispatcher to the current thread
            m_disp = Dispatcher.AttachWorkerDispatcher();
            m_disp.BeginInvoke(new Action(Init));
            m_disp.Run();
        }

        /// <summary>
        /// Initialize TT API
        /// </summary>
        public void Init()
        {
            // Use "Universal Login" Login Mode
            TTAPI.UniversalLoginModeDelegate ulDelegate = new TTAPI.UniversalLoginModeDelegate(ttApiInitComplete);
            TTAPI.CreateUniversalLoginTTAPI(Dispatcher.Current, ulDelegate);
        }

        /// <summary>
        /// Event notification for status of TT API initialization
        /// </summary>
        public void ttApiInitComplete(UniversalLoginTTAPI api, Exception ex)
        {
            if (ex == null)
            {
                // Authenticate your credentials
                m_apiInstance = api;
                m_apiInstance.AuthenticationStatusUpdate += new EventHandler<AuthenticationStatusUpdateEventArgs>(apiInstance_AuthenticationStatusUpdate);
                m_apiInstance.Authenticate("USERNAME", "PASSWORD");
            }
            else
            {
                Console.WriteLine("TT API Initialization Failed: {0}", ex.Message);
                Dispose();
            }
        }

        /// <summary>
        /// Event notification for status of authentication
        /// </summary>
        public void apiInstance_AuthenticationStatusUpdate(object sender, AuthenticationStatusUpdateEventArgs e)
        {
            if (e.Status.IsSuccess)
            {
                // lookup an instrument
                m_req = new InstrumentLookupSubscription(m_apiInstance.Session, Dispatcher.Current,
                    new ProductKey(MarketKey.Cme, ProductType.Future, "ES"),
                    "Mar13");
                m_req.Update += new EventHandler<InstrumentLookupSubscriptionEventArgs>(m_req_Update);
                m_req.Start();
            }
            else
            {
                Console.WriteLine("TT Login failed: {0}", e.Status.StatusMessage);
                Dispose();
            }
        }

        /// <summary>
        /// Event notification for instrument lookup
        /// </summary>
        void m_req_Update(object sender, InstrumentLookupSubscriptionEventArgs e)
        {
            if (e.Instrument != null && e.Error == null)
            {
                // Instrument was found
                Console.WriteLine("Found: {0}", e.Instrument.Name);

                // Subscribe for Time & Sales Data
                m_ts = new TimeAndSalesSubscription(e.Instrument, Dispatcher.Current);
                m_ts.Update += new EventHandler<TimeAndSalesEventArgs>(m_ts_Update);
                m_ts.Start();
            }
            else if (e.IsFinal)
            {
                // Instrument was not found and TT API has given up looking for it
                Console.WriteLine("Cannot find instrument: {0}", e.Error.Message);
                Dispose();
            }
        }

        /// <summary>
        /// Event notification for Time & Sales update
        /// </summary>
        void m_ts_Update(object sender, TimeAndSalesEventArgs e)
        {
            if (e.Error == null)
            {
                // More than one LTP/LTQ may be received in a single event
                foreach (TimeAndSalesData tsData in e.Data)
                {
                    Price ltp = tsData.TradePrice;
                    Quantity ltq = tsData.TradeQuantity;
                    Console.WriteLine("LTP = {0} : LTQ = {1}", ltp.ToString(), ltq.ToInt());
                }
            }
        }

        /// <summary>
        /// Shuts down the TT API
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposing pattern implementation
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Shutdown all subscriptions
                    if (m_ts != null)
                    {
                        m_ts.Dispose();
                        m_ts = null;
                    }
                    if (m_req != null)
                    {
                        m_req.Dispose();
                        m_req = null;
                    }

                    // Shutdown the Dispatcher
                    if (m_disp != null)
                    {
                        m_disp.BeginInvokeShutdown();
                        m_disp = null;
                    }

                    // Shutdown the TT API
                    if (m_apiInstance != null)
                    {
                        m_apiInstance.Shutdown();
                        m_apiInstance = null;
                    }
                }
            }

            m_disposed = true;
        }
    }
}
