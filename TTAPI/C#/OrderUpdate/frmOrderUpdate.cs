﻿// **********************************************************************************************************************
//
//	Copyright © 2005-2013 Trading Technologies International, Inc.
//	All Rights Reserved Worldwide
//
// 	* * * S T R I C T L Y   P R O P R I E T A R Y * * *
//
//  WARNING: This file and all related programs (including any computer programs, example programs, and all source code) 
//  are the exclusive property of Trading Technologies International, Inc. (“TT”), are protected by copyright law and 
//  international treaties, and are for use only by those with the express written permission from TT.  Unauthorized 
//  possession, reproduction, distribution, use or disclosure of this file and any related program (or document) derived 
//  from it is prohibited by State and Federal law, and by local law outside of the U.S. and may result in severe civil 
//  and criminal penalties.
//
// ************************************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using TradingTechnologies.TTAPI;
using TradingTechnologies.TTAPI.WinFormsHelpers;
using TradingTechnologies.TTAPI.Tradebook;

namespace TTAPI_Samples
{
    /// <summary>
    /// OrderUpdate
    /// 
    /// This example demonstrates using the TT API to retrieve order status updates 
    /// from the TradeSubscription.  This sample uses UniversalLoginTTAPI for 
    /// authentication.
    /// </summary>
    public partial class frmOrderUpdate : Form
    {
        // Declare the TTAPI objects.
        private UniversalLoginTTAPI m_TTAPI = null;
        private TradeSubscription m_TradeSubscription = null;

        public frmOrderUpdate()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Assign the TT API variable
        /// </summary>
        /// <param name="instance">UniversalLoginTTAPI instance</param>
        /// <param name="ex">Any exception generated from the UniversalLoginModeDelegate</param>
        public void initTTAPI(UniversalLoginTTAPI apiInstance, Exception ex)
        {
            m_TTAPI = apiInstance;
        }

        /// <summary>
        /// Finalize the GUI and instantiate the start the trade subscription 
        /// </summary>
        private void frmMonitorOrders_Load(object sender, EventArgs e)
        {
            // initialize columns to have equal width
            int columnWidth = Convert.ToInt32(this.lboAuditLog.ClientRectangle.Width / 10);

            // add our columns to the listbox
            this.lboAuditLog.Columns.Add("Event", columnWidth);
            this.lboAuditLog.Columns.Add("Account", columnWidth);
            this.lboAuditLog.Columns.Add("Order Status", columnWidth);
            this.lboAuditLog.Columns.Add("Order Action", columnWidth);
            this.lboAuditLog.Columns.Add("Buy/Sell", columnWidth);
            this.lboAuditLog.Columns.Add("Order Qty", columnWidth);
            this.lboAuditLog.Columns.Add("Working Qty", columnWidth);
            this.lboAuditLog.Columns.Add("Price", columnWidth);
            this.lboAuditLog.Columns.Add("SiteOrderKey", columnWidth);
            this.lboAuditLog.Columns.Add("Order#", columnWidth);
        }

        /// <summary>
        /// Attempt to authenticate the user.
        /// </summary>
        private void btnConnect_Click(object sender, EventArgs e)
        {
            // Check to ensure that values have been entered for all fields
            if (txtUsername.Text == String.Empty)
            {
                MessageBox.Show("Please enter a username");
                return;
            }
            if (txtPassword.Text == String.Empty)
            {
                MessageBox.Show("Please enter a Password");
                return;
            }

            // Login using the user-entered information.  Register for the authentication udpate event.
            m_TTAPI.AuthenticationStatusUpdate += new EventHandler<AuthenticationStatusUpdateEventArgs>(m_TTAPI_AuthenticationStatusUpdate);
            m_TTAPI.Authenticate(this.txtUsername.Text, this.txtPassword.Text);
        }

        /// <summary>
        /// Event returning the status of a login attempt.
        /// </summary>
        void m_TTAPI_AuthenticationStatusUpdate(object sender, AuthenticationStatusUpdateEventArgs e)
        {
            // Check for successful login
            if (!e.Status.IsSuccess)
            {
                // if an unsuccessful login, show the user why and allow them to try again
                MessageBox.Show("Login Unsuccessful.\r\n\r\nReason: " + e.Status.StatusMessage, "Error");
                return;
            }

            // subscribe to the trade subscription events
            m_TradeSubscription = new TradeSubscription(m_TTAPI.Session, Dispatcher.Current);
            m_TradeSubscription.OrderAdded += new EventHandler<OrderAddedEventArgs>(tradeSubscription_OrderAdded);
            m_TradeSubscription.OrderBookDownload += new EventHandler<OrderBookDownloadEventArgs>(tradeSubscription_OrderBookDownload);
            m_TradeSubscription.OrderDeleted += new EventHandler<OrderDeletedEventArgs>(tradeSubscription_OrderDeleted);
            m_TradeSubscription.OrderFilled += new EventHandler<OrderFilledEventArgs>(tradeSubscription_OrderFilled);
            m_TradeSubscription.OrderRejected += new EventHandler<OrderRejectedEventArgs>(tradeSubscription_OrderRejected);
            m_TradeSubscription.OrderUpdated += new EventHandler<OrderUpdatedEventArgs>(tradeSubscription_OrderUpdated);
            m_TradeSubscription.OrderStatusUnknown += new EventHandler<OrderStatusUnknownEventArgs>(tradeSubscription_OrderStatusUnknown);
            m_TradeSubscription.Start();

            // Login succeeded, update GUI
            this.btnConnect.Enabled = false;
            UpdateStatusBar("Login Status: " + e.Status.ToString());
        }

        /// <summary>
        /// Triggered when the TT API cannot determine the status of an Order
        /// </summary>
        void tradeSubscription_OrderStatusUnknown(object sender, OrderStatusUnknownEventArgs e)
        {
            UpdateAuditLog("OrderStatusUnknown", e.Order);
        }

        /// <summary>
        /// Triggered when a Order in this subscription is updated. 
        /// </summary>
        void tradeSubscription_OrderUpdated(object sender, OrderUpdatedEventArgs e)
        {
            UpdateAuditLog("OrderUpdated", e.NewOrder);
        }

        /// <summary>
        /// Triggered when the exchange rejects an order action
        /// </summary>
        void tradeSubscription_OrderRejected(object sender, OrderRejectedEventArgs e)
        {
            UpdateAuditLog("OrderRejected", e.Order);
        }

        /// <summary>
        /// Triggered when a list of Orders is received during a order download initiated as a result 
        /// of calling the subscription's Start method 
        /// </summary>
        void tradeSubscription_OrderBookDownload(object sender, OrderBookDownloadEventArgs e)
        {
            foreach (Order order in e.Orders)
                UpdateAuditLog("OrderBookDownload", order);
        }

        /// <summary>
        /// Triggered when a new Order in the trade subscription gets filled 
        /// </summary>
        void tradeSubscription_OrderFilled(object sender, OrderFilledEventArgs e)
        {
            UpdateAuditLog("OrderFilled", e.NewOrder);
        }

        /// <summary>
        /// Triggered when an Order monitored by this subsciption is deleted 
        /// </summary>
        void tradeSubscription_OrderDeleted(object sender, OrderDeletedEventArgs e)
        {
            UpdateAuditLog("OrderDeleted", e.DeletedUpdate);
        }

        /// <summary>
        /// Triggered when a new Order is received
        /// </summary>
        void tradeSubscription_OrderAdded(object sender, OrderAddedEventArgs e)
        {
            UpdateAuditLog("OrderAdded", e.Order);
        }

        /// <summary>
        /// Publish the order data to the GUI
        /// </summary>
        /// <param name="eventName">Event which fired the order update</param>
        /// <param name="order">Order which has updated</param>
        private void UpdateAuditLog(string eventName, Order order)
        {
            ListViewItem item = new ListViewItem();

            item.Text = eventName;
            item.SubItems.Add(order.AccountName.ToString());
            item.SubItems.Add(order.Status.ToString());
            item.SubItems.Add(order.Action.ToString());
            item.SubItems.Add(order.BuySell.ToString());
            item.SubItems.Add(order.OrderQuantity.ToString());
            item.SubItems.Add(order.WorkingQuantity.ToString());
            item.SubItems.Add(order.LimitPrice.ToString());
            item.SubItems.Add(order.SiteOrderKey);
            item.SubItems.Add(order.OrderNumber.ToString());

            this.lboAuditLog.Items.Add(item);
        }

        /// <summary>
        /// Event which opens the About window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mnuAbout_Click(object sender, EventArgs e)
        {
            AboutDTS aboutForm = new AboutDTS();
            aboutForm.ShowDialog(this);
        }

        #region UpdateStatusBar

        /// <summary>
        /// Update the status bar and write the message to the console in a thread safe way.
        /// </summary>
        /// <param name="message">Message to update the status bar with.</param>
        delegate void UpdateStatusBarCallback(string message);
        public void UpdateStatusBar(string message)
        {
            if (this.InvokeRequired)
            {
                UpdateStatusBarCallback statCB = new UpdateStatusBarCallback(UpdateStatusBar);
                this.Invoke(statCB, new object[] { message });
            }
            else
            {
                // Update the status bar.
                toolStripStatusLabel1.Text = message;

                // Also write this message to the console.
                Console.WriteLine(message);
            }
        }

        #endregion

    }
}