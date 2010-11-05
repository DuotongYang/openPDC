﻿//******************************************************************************************************
//  BrowseDevicesUserControl.xaml.cs - Gbtc
//
//  Copyright © 2010, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  07/19/2010 - Mehulbhai P Thakkar
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using openPDCManager.Data;
using openPDCManager.Data.Entities;
using openPDCManager.ModalDialogs;
using openPDCManager.Pages.Manage;
using openPDCManager.UserControls.CommonControls;
using openPDCManager.Utilities;
using openPDCManager.Data.ServiceCommunication;
using System.Collections.ObjectModel;

namespace openPDCManager.Pages.Devices
{
    /// <summary>
    /// Interaction logic for Browse.xaml
    /// </summary>
    public partial class BrowseDevicesUserControl : UserControl
    {
        #region [ Members ]
                
        List<Device> m_deviceList = new List<Device>();        
        ActivityWindow m_activityWindow;
        
        #endregion

        #region [ Constructor ]

        public BrowseDevicesUserControl()
        {            
            InitializeComponent();
            ButtonSearch.Content = new BitmapImage(new Uri(@"images/Search.png", UriKind.Relative));
            ButtonShowAll.Content = new BitmapImage(new Uri(@"images/CancelSearch.png", UriKind.Relative));                       
            UpdateLayout();

            Loaded += new RoutedEventHandler(Browse_Loaded);
            ButtonSearch.Click += new RoutedEventHandler(ButtonSearch_Click);
            ButtonShowAll.Click += new RoutedEventHandler(ButtonShowAll_Click);            
        }

        #endregion

        #region [ Control Event Handlers ]

        void ButtonShowAll_Click(object sender, RoutedEventArgs e)
        {
            //Storyboard sb = new Storyboard();
            //sb = Application.Current.Resources["ButtonPressAnimation"] as Storyboard;
            //sb.Completed += new EventHandler(delegate(object obj, EventArgs es) { sb.Stop(); });
            //Storyboard.SetTarget(sb, ButtonShowAllTransform);
            //sb.Begin();
            //m_activityWindow = new ActivityWindow("Loading Data... Please Wait...");
            //m_activityWindow.Owner = Window.GetWindow(this);
            //m_activityWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //m_activityWindow.Show();
            BindData(m_deviceList);
        }

        void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            //Storyboard sb = new Storyboard();
            //sb = Application.Current.Resources["ButtonPressAnimation"] as Storyboard;
            //sb.Completed += new EventHandler(delegate(object obj, EventArgs es) { sb.Stop(); });
            //Storyboard.SetTarget(sb, ButtonSearchTransform);
            //sb.Begin();

            string searchText = TextBoxSearch.Text.ToUpper();
            List<Device> searchResult = new List<Device>();
            searchResult = (from item in m_deviceList
                            where item.Acronym.ToUpper().Contains(searchText) || item.Name.ToUpper().Contains(searchText) || item.ProtocolName.ToUpper().Contains(searchText)
                               || item.InterconnectionName.ToUpper().Contains(searchText) || item.CompanyName.ToUpper().Contains(searchText) || item.VendorDeviceName.ToUpper().Contains(searchText)
                            select item).ToList();
            BindData(searchResult);
        }

        void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            Device device = ((Button)sender).DataContext as Device;
            
            ManageDevicesUserControl manageDevicesUserControl = new ManageDevicesUserControl();
            manageDevicesUserControl.m_deviceID = device.ID;
            manageDevicesUserControl.m_oldAcronym = device.Acronym;
            ((MasterLayoutWindow)Window.GetWindow(this)).ContentFrame.Navigate(manageDevicesUserControl);            
        }

        void HyperlinkButtonPhasors_Click(object sender, RoutedEventArgs e)
        {
            int deviceId = Convert.ToInt32(((Button)sender).Tag.ToString());
            string acronym = ((Button)sender).ToolTip.ToString();
            Phasors phasors = new Phasors(deviceId, acronym);
            phasors.Owner = Window.GetWindow(this);
            phasors.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            phasors.ShowDialog();
        }

        void HyperlinkButtonMeasurements_Click(object sender, RoutedEventArgs e)
        {
            string deviceId = ((Button)sender).Tag.ToString();
            Measurements measurements = new Measurements(Convert.ToInt32(deviceId));
            ((MasterLayoutWindow)Window.GetWindow(this)).ContentFrame.Navigate(measurements);
        }

        void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            SystemMessages sm;
            try
            {
                Device device = new Device();
                device = ((Button)sender).DataContext as Device;
                string result = CommonFunctions.SaveDevice(null, device, false, 0, 0);
                sm = new SystemMessages(new Message() { UserMessage = result, SystemMessage = string.Empty, UserMessageType = MessageType.Success },
                        ButtonType.OkOnly);

                //Update Metadata in the openPDC Service.                
                try
                {
                    WindowsServiceClient serviceClient = ((App)Application.Current).ServiceClient;
                    if (serviceClient != null && serviceClient.Helper.RemotingClient.CurrentState == TVA.Communication.ClientState.Connected)
                    {
                        if (device.Enabled) //if device is enabled then send initialize command otherwise send reloadconfig command.
                        {
                            if (device.ParentID == null)
                                CommonFunctions.SendCommandToWindowsService(serviceClient, "Initialize " + CommonFunctions.GetRuntimeID(null, "Device", device.ID));
                            else
                                CommonFunctions.SendCommandToWindowsService(serviceClient, "Initialize " + CommonFunctions.GetRuntimeID(null, "Device", (int)device.ParentID));
                        }
                        else
                            CommonFunctions.SendCommandToWindowsService(serviceClient, "ReloadConfig"); //we do this to make sure all statistical measurements are in the system.

                        if (device.HistorianID != null)
                        {
                            string runtimeID = CommonFunctions.GetRuntimeID(null, "Historian", (int)device.HistorianID);
                            CommonFunctions.SendCommandToWindowsService(serviceClient, "Invoke " + runtimeID + " refreshmetadata");
                        }

                        //now also update Stat historian metadata.
                        Historian statHistorian = CommonFunctions.GetHistorianByAcronym(null, "STAT");
                        if (statHistorian != null)
                        {
                            string statRuntimeID = CommonFunctions.GetRuntimeID(null, "Historian", statHistorian.ID);
                            if (serviceClient != null && serviceClient.Helper.RemotingClient.CurrentState == TVA.Communication.ClientState.Connected)
                                CommonFunctions.SendCommandToWindowsService(serviceClient, "Invoke " + statRuntimeID + " refreshmetadata");
                        }

                        CommonFunctions.SendCommandToWindowsService(serviceClient, "Invoke 0 ReloadStatistics");
                    }
                    else
                    {                     
                        sm = new SystemMessages(new openPDCManager.Utilities.Message() { UserMessage = "Failed to Perform Configuration Changes", SystemMessage = "Application is disconnected from the openPDC Service.", UserMessageType = openPDCManager.Utilities.MessageType.Information }, ButtonType.OkOnly);
                        sm.Owner = Window.GetWindow(this);
                        sm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        sm.ShowPopup();
                    }
                }
                catch (Exception ex)
                {
                    SystemMessages sm1 = new SystemMessages(new openPDCManager.Utilities.Message() { UserMessage = "Failed to Perform Configuration Changes", SystemMessage = ex.Message, UserMessageType = openPDCManager.Utilities.MessageType.Information }, ButtonType.OkOnly);
                    sm1.Owner = Window.GetWindow(this);
                    sm1.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    sm1.ShowPopup();
                    CommonFunctions.LogException(null, "ButtonSave_Click.RefreshMetadata", ex);
                }

            }
            catch (Exception ex)
            {
                CommonFunctions.LogException(null, "WPF.SaveDevice", ex);
                sm = new SystemMessages(new Message() { UserMessage = "Failed to Save Device Information", SystemMessage = ex.Message, UserMessageType = MessageType.Error },
                        ButtonType.OkOnly);
            }
            sm.Owner = Window.GetWindow(this);
            sm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            sm.ShowPopup();
            RefreshDeviceList();
        }

        void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            SystemMessages sm;
            try
            {
                string result;
                Device device = new Device();
                device = ((Button)sender).DataContext as Device;

                if (device.IsConcentrator)
                    sm = new SystemMessages(new Message() { UserMessage = "Do you want to delete concentrator device?", SystemMessage = "Device Acronym: " + device.Acronym + Environment.NewLine + "Deleting concentrator will also delete " + CommonFunctions.GetDeviceListByParentID(null, device.ID).Count() + " associated device(s).", UserMessageType = MessageType.Confirmation }, ButtonType.YesNo);
                else
                    sm = new SystemMessages(new Message() { UserMessage = "Do you want to delete device?", SystemMessage = "Device Acronym: " + device.Acronym, UserMessageType = MessageType.Confirmation }, ButtonType.YesNo);
                
                sm.Closed += new EventHandler(delegate(object popupWindow, EventArgs eargs)
                {
                    if ((bool)sm.DialogResult)
                    {
                        if (device.IsConcentrator)
                        {
                            List<Device> deviceList = CommonFunctions.GetDeviceListByParentID(null, device.ID);
                            foreach (Device d in deviceList)
                                CommonFunctions.DeleteDevice(null, d.ID);
                            result = CommonFunctions.DeleteDevice(null, device.ID);
                        }
                        else
                            result = CommonFunctions.DeleteDevice(null, device.ID);                    
                        
                        SystemMessages sm1 = new SystemMessages(new Message() { UserMessage = result, SystemMessage = string.Empty, UserMessageType = MessageType.Success },
                            ButtonType.OkOnly);
                        sm1.Owner = Window.GetWindow(this);
                        sm1.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        sm1.ShowPopup();
                        RefreshDeviceList();

                        //Update Metadata in the openPDC Service.                
                        try
                        {
                            WindowsServiceClient serviceClient = ((App)Application.Current).ServiceClient;
                            if (serviceClient != null && serviceClient.Helper.RemotingClient.CurrentState == TVA.Communication.ClientState.Connected)
                            {                                
                                if (device.ParentID == null)
                                    CommonFunctions.SendCommandToWindowsService(serviceClient, "ReloadConfig");
                                else
                                    CommonFunctions.SendCommandToWindowsService(serviceClient, "Initialize " + CommonFunctions.GetRuntimeID(null, "Device", (int)device.ParentID));                                
                            }
                        }
                        catch (Exception ex)
                        {
                            CommonFunctions.LogException(null, "ButtonSave_Click.RefreshMetadata", ex);
                        }

                    }
                });                
            }
            catch (Exception ex)
            {
                CommonFunctions.LogException(null, "WPF.DeleteDevice", ex);
                sm = new SystemMessages(new Message() { UserMessage = "Failed to Delete Device", SystemMessage = ex.Message, UserMessageType = MessageType.Error },
                        ButtonType.OkOnly);                
            }
            sm.Owner = Window.GetWindow(this);
            sm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            sm.ShowPopup();
        }

        void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            SystemMessages sm;
            try
            {
                Device device = new Device();
                device = ((Button)sender).DataContext as Device;
                sm = new SystemMessages(new Message() { UserMessage = "Do you want to make a copy of " + device.Acronym + " device?", SystemMessage = "This will copy device configuration and generate new measurements.", UserMessageType = MessageType.Confirmation }, ButtonType.YesNo);
                sm.Closed += new EventHandler(delegate(object popupWindow, EventArgs eargs)
                {
                    if ((bool)sm.DialogResult)
                    {
                        string originalAcronym = device.Acronym;
                        int i = 1;
                        do
                        {
                            if (originalAcronym.Length > 15 && i < 10)
                                device.Acronym = originalAcronym.Substring(0, 14) + i.ToString();
                            else if (originalAcronym.Length > 15 && i >= 10)
                                device.Acronym = originalAcronym.Substring(0, 13) + i.ToString();
                            else
                                device.Acronym = originalAcronym + i.ToString();

                            i++;
                        } while (CommonFunctions.GetDeviceByAcronym(null, device.Acronym) != null);

                        device.Name = "Copy of " + device.Name;
                        device.Enabled = false;
                        ManageDevicesUserControl manageDevice = new ManageDevicesUserControl(device);
                        ((MasterLayoutWindow)Window.GetWindow(this)).ContentFrame.Navigate(manageDevice);                                           
                    }
                });
            }
            catch (Exception ex)
            {
                CommonFunctions.LogException(null, "WPF.CopyDevice", ex);
                sm = new SystemMessages(new Message() { UserMessage = "Failed to Create Copy of Device", SystemMessage = ex.Message, UserMessageType = MessageType.Error },
                        ButtonType.OkOnly);
            }
            sm.Owner = Window.GetWindow(this);
            sm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            sm.ShowPopup();
        }

        private void HyperlinkButtonCreatedOn_Click(object sender, RoutedEventArgs e)
        {
            //SortData("CreatedOn");
            List<Device> sortedList = new List<Device>();
            sortedList = (from device in m_deviceList
                          select device).ToList().OrderBy(d => d.CreatedOn).ToList();
            ListBoxDeviceList.ItemsSource = sortedList;
        }

        private void HyperlinkButtonAcronym_Click(object sender, RoutedEventArgs e)
        {
            //SortData("Acronym");
            List<Device> sortedList = new List<Device>();
            sortedList = (from device in m_deviceList
                          select device).ToList().OrderBy(d => d.Acronym).ToList();
            ListBoxDeviceList.ItemsSource = sortedList;
        }

        private void HyperlinkButtonName_Click(object sender, RoutedEventArgs e)
        {
            List<Device> sortedList = new List<Device>();
            sortedList = (from device in m_deviceList
                          select device).ToList().OrderBy(d => d.Name).ToList();
            ListBoxDeviceList.ItemsSource = sortedList;
        }

        private void HyperlinkButtonConcentrator_Click(object sender, RoutedEventArgs e)
        {
            List<Device> sortedList = new List<Device>();
            sortedList = (from device in m_deviceList
                          select device).ToList().OrderBy(d => d.IsConcentrator).ToList();
            ListBoxDeviceList.ItemsSource = sortedList;
        }

        private void HyperlinkButtonProtocol_Click(object sender, RoutedEventArgs e)
        {
            List<Device> sortedList = new List<Device>();
            sortedList = (from device in m_deviceList
                          select device).ToList().OrderBy(d => d.ProtocolName).ToList();
            ListBoxDeviceList.ItemsSource = sortedList;
        }

        private void HyperlinkButtonCompany_Click(object sender, RoutedEventArgs e)
        {
            List<Device> sortedList = new List<Device>();
            sortedList = (from device in m_deviceList
                          select device).ToList().OrderBy(d => d.CompanyAcronym).ToList();
            ListBoxDeviceList.ItemsSource = sortedList;
        }

        private void HyperlinkButtonEnabled_Click(object sender, RoutedEventArgs e)
        {
            List<Device> sortedList = new List<Device>();
            sortedList = (from device in m_deviceList
                          select device).ToList().OrderBy(d => d.Enabled).ToList();
            ListBoxDeviceList.ItemsSource = sortedList;
        }
        
        #endregion

        #region [ Page Event Handlers ]

        void Browse_Loaded(object sender, RoutedEventArgs e)
        {
            m_activityWindow = new ActivityWindow("Loading Data... Please Wait...");
            m_activityWindow.Owner = Window.GetWindow(this);
            m_activityWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            m_activityWindow.Show();
            RefreshDeviceList();
        }

        #endregion

        #region [ Methods ]

        void RefreshDeviceList()
        {
            App app = (App)Application.Current;
            string nodeID = app.NodeValue;
            try
            {
                m_deviceList = CommonFunctions.GetDeviceList(null, nodeID);             
                BindData(m_deviceList);                
            }
            catch (Exception ex)
            {
                CommonFunctions.LogException(null, "WPF.GetDeviceList", ex);
                SystemMessages sm = new SystemMessages(new Message() { UserMessage = "Failed to Retrieve Device List", SystemMessage = ex.Message, UserMessageType = MessageType.Error },
                        ButtonType.OkOnly);
                sm.Owner = Window.GetWindow(this);
                sm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sm.ShowPopup();
            }
            if (m_activityWindow != null)
                m_activityWindow.Close();
        }

        void BindData(List<Device> deviceList)
        {            
            //ListBoxDeviceList.ItemsSource = deviceList;                 
            if (deviceList.Count > 0)
                DataPagerDevices.ItemsSource = new ObservableCollection<Object>(deviceList);
            else
            {
                DataPagerDevices.ItemsSource = null;
                ListBoxDeviceList.Items.Refresh();
            }
        }

        #endregion       
    }
}
