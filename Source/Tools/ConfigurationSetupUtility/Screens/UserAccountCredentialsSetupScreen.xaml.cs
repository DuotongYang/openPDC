﻿//******************************************************************************************************
//  UserAccountCredentialsSetupScreen.xaml.cs - Gbtc
//
//  Copyright © 2011, Grid Protection Alliance.  All Rights Reserved.
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
//  01/23/2011 - J. Ritchie Carroll
//       Generated original version of source code.
//  02/28/2011 - Mehulbhai P Thakkar
//       Added a checkbox to allow pass-through authentication.
//       Added SetFocus() method to set intial focus for better user experience.
//       Added TextBox_GotFocus() event for all textboxes to highlight current value in the textbox.
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TVA.Identity;

namespace ConfigurationSetupUtility.Screens
{
    /// <summary>
    /// Interaction logic for UserAccountCredentialsSetupScreen.xaml
    /// </summary>
    public partial class UserAccountCredentialsSetupScreen : UserControl, IScreen
    {
        #region [ Members ]

        // Fields
        private Dictionary<string, object> m_state;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="UserAccountCredentialsSetupScreen"/> class.
        /// </summary>
        public UserAccountCredentialsSetupScreen()
        {
            InitializeComponent();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the screen to be displayed when the user clicks the "Next" button.
        /// </summary>
        public IScreen NextScreen
        {
            get
            {
                IScreen applyChangesScreen;

                if (!State.ContainsKey("applyChangesScreen"))
                    State.Add("applyChangesScreen", new ApplyConfigurationChangesScreen());

                applyChangesScreen = State["applyChangesScreen"] as IScreen;

                return applyChangesScreen;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the user can advance to
        /// the next screen from the current screen.
        /// </summary>
        public bool CanGoForward
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the user can return to
        /// the previous screen from the current screen.
        /// </summary>
        public bool CanGoBack
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the user can cancel the
        /// setup process from the current screen.
        /// </summary>
        public bool CanCancel
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the user input is valid on the current page.
        /// </summary>
        public bool UserInputIsValid
        {
            get
            {
                if ((bool)RadioButtonWindowsAuthentication.IsChecked)
                {
                    string errorMessage = string.Empty;
                    try
                    {
                        string[] userData = m_userNameTextBox.Text.Split(new char[] { '\\' });

                        if (userData.Length == 2)
                        {
                            if (UserInfo.AuthenticateUser(userData[0], userData[1], m_userPasswordTextBox.Password.Trim(), out errorMessage) == null)
                            {
                                MessageBox.Show("Authentication failed. Please verify your username and password.", "Verifying Windows Credentials");
                                return false;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Username format is invalid: for Windows authentication please provide a username formatted like domain\\username.\r\nUse the machine name \"" + Environment.MachineName + "\" as the domain name if the system is not on a domain or you want to use a local account.", "Verifying Windows Credentials");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message + Environment.NewLine + errorMessage, "Verifying Windows Credentials - ERROR!");
                        return false;
                    }
                }
                else
                {
                    string passwordRequirementRegex = "^.*(?=.{8,})(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).*$";
                    string passwordRequirementError = "Invalid Password: Password must be at least 8 characters; must contain at least 1 number, 1 upper case letter, and 1 lower case letter";

                    string userName = m_userNameTextBox.Text.Trim();
                    string password = m_userPasswordTextBox.Password.Trim();
                    string confirmPassword = m_userConfirmPasswordTextBox.Password.Trim();
                    string firstName = m_userFirstNameTextBox.Text.Trim();
                    string lastName = m_userLastNameTextBox.Text.Trim();

                    if (string.IsNullOrEmpty(userName))
                    {
                        MessageBox.Show("Please provide administrative user account name.", "Database User Credentials");
                        m_userNameTextBox.Focus();
                        return false;
                    }
                    else if (string.IsNullOrEmpty(password) || !Regex.IsMatch(password, passwordRequirementRegex))
                    {
                        MessageBox.Show("Please provide valid password for administrative user." + Environment.NewLine + passwordRequirementError, "Database User Credentials");
                        m_userPasswordTextBox.Focus();
                        return false;
                    }
                    else if (password != confirmPassword)
                    {
                        MessageBox.Show("Password does not match the cofirm password", "Database User Credentials");
                        m_userConfirmPasswordTextBox.SelectAll();
                        m_userConfirmPasswordTextBox.Focus();
                        return false;
                    }
                    else if (string.IsNullOrEmpty(m_userFirstNameTextBox.Text.Trim()))
                    {
                        MessageBox.Show("Please provide first name for administrative user", "Database User Credentials");
                        m_userFirstNameTextBox.Focus();
                        return false;
                    }
                    else if (string.IsNullOrEmpty(m_userLastNameTextBox.Text.Trim()))
                    {
                        MessageBox.Show("Please provide last name for administrative user", "Database User Credentials");
                        m_userLastNameTextBox.Focus();
                        return false;
                    }
                }

                // Update state values to the latest entered on the form.
                InitializeState();
                return true;
            }
        }

        private SecureString ConvertToSecureString(string value)
        {
            SecureString ret = new SecureString();

            foreach (char c in value)
            {
                ret.AppendChar(c);
            }

            ret.MakeReadOnly();

            return ret;
        }

        /// <summary>
        /// Collection shared among screens that represents the state of the setup.
        /// </summary>
        public Dictionary<string, object> State
        {
            get
            {
                return m_state;
            }
            set
            {
                m_state = value;
                InitializeState();
            }
        }

        /// <summary>
        /// Allows the screen to update the navigation buttons after a change is made
        /// that would affect the user's ability to navigate to other screens.
        /// </summary>
        public Action UpdateNavigation
        {
            get;
            set;
        }

        #endregion

        #region [ Methods ]

        // Initializes the state keys to their default values.
        private void InitializeState()
        {
            if (m_state != null)
            {
                m_state["authenticationType"] = (bool)RadioButtonWindowsAuthentication.IsChecked ? "windows" : "database";
                m_state["adminUserName"] = m_userNameTextBox.Text.Trim();
                m_state["adminPassword"] = m_userPasswordTextBox.Password.Trim();
                m_state["adminUserFirstName"] = m_userFirstNameTextBox.Text.Trim();
                m_state["adminUserLastName"] = m_userLastNameTextBox.Text.Trim();
                m_state["allowPassThroughAuthentication"] = (bool)m_checkBoxPassThroughAuthentication.IsChecked ? "True" : "False";
            }
        }

        private void RadioButtonWindowsAuthentication_Checked(object sender, RoutedEventArgs e)
        {
            //i.e. Windows Authentication Selected.            
            m_messageTextBlock.Text = "Please enter current credentials for active directory user to be the administrator for openPDC. Credentials will be validated by operating system.";
            m_userAccountHeaderTextBlock.Text = "Windows Authentication";
            m_userNameTextBox.Text = Thread.CurrentPrincipal.Identity.Name;
            m_dbInfoGrid.Visibility = Visibility.Collapsed;
            m_checkBoxPassThroughAuthentication.Visibility = Visibility.Visible;
            m_textBlockPassThroughMessage.Visibility = Visibility.Visible;
            SetFocus();

        }

        private void RadioButtonWindowsAuthentication_Unchecked(object sender, RoutedEventArgs e)
        {
            //i.e. Database Authentication Selected.
            m_messageTextBlock.Text = "Please provide the desired credentials for database user to be the administrator for openPDC.";
            m_userAccountHeaderTextBlock.Text = "Database Authentication";
            m_userNameTextBox.Text = string.Empty;
            m_dbInfoGrid.Visibility = Visibility.Visible;
            m_checkBoxPassThroughAuthentication.Visibility = Visibility.Collapsed;
            m_textBlockPassThroughMessage.Visibility = Visibility.Collapsed;
            SetFocus();
        }

        private void UserAccountCredentialsSetupScreen_Loaded(object sender, RoutedEventArgs e)
        {            
            RadioButtonWindowsAuthentication.IsChecked = true;
            m_userNameTextBox.Text = Thread.CurrentPrincipal.Identity.Name;
            SetFocus();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox)
                ((TextBox)sender).SelectAll();
            else if (sender is PasswordBox)
                ((PasswordBox)sender).SelectAll();
        }

        private void SetFocus()
        {
            if (!string.IsNullOrEmpty(m_userNameTextBox.Text))
            {
                m_userPasswordTextBox.Focus();
            }
            else
            {
                m_userNameTextBox.SelectAll();
                m_userNameTextBox.Focus();
            }
        }
        #endregion

    }
}
