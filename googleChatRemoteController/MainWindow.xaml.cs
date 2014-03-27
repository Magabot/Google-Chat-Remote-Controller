using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using agsXMPP;

namespace googleChatRemoteController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        XmppClientConnection googleChat = new XmppClientConnection();

        private Dispatcher _dispatcher;
        System.Windows.Threading.DispatcherTimer frameTimer;

        bool expanded = true;

        String lastMessageSent;
        String lastMessageSender;

        bool pressStop = false;

        public MainWindow()
        {
            InitializeComponent();

            _dispatcher = this.Dispatcher;

            frameTimer = new System.Windows.Threading.DispatcherTimer();
            frameTimer.Tick += updateController;
            frameTimer.Interval = TimeSpan.FromSeconds(1.0 / 60.0);
            frameTimer.Start();

            listGoogleChatEvents.Items.Clear();

            // Subscribe to Events
            googleChat.OnLogin += new ObjectHandler(googleChat_OnLogin);
            googleChat.OnRosterStart += new ObjectHandler(googleChat_OnRosterStart);
            googleChat.OnRosterEnd += new ObjectHandler(googleChat_OnRosterEnd);
            googleChat.OnRosterItem += new XmppClientConnection.RosterHandler(googleChat_OnRosterItem);
            googleChat.OnPresence += new agsXMPP.protocol.client.PresenceHandler(googleChat_OnPresence);
            googleChat.OnAuthError += new XmppElementHandler(googleChat_OnAuthError);
            googleChat.OnError += new ErrorHandler(googleChat_OnError);
            googleChat.OnClose += new ObjectHandler(googleChat_OnClose);
            googleChat.OnMessage += new agsXMPP.protocol.client.MessageHandler(googleChat_OnMessage);

            Properties.Settings.Default.Reload();
            passwordBoxGoogleChat.Password = Properties.Settings.Default.password;
            textBoxGoogleChatUsername.Text = Properties.Settings.Default.email;
        }


        #region Sign In
        private void buttonSignIn_Click(object sender, RoutedEventArgs e)
        {
            String email = "";

            if (textBoxGoogleChatUsername.Text.Contains('@'))
            {
                if (textBoxGoogleChatUsername.Text.Contains("@gmail.com"))
                {
                    email = textBoxGoogleChatUsername.Text;
                }
                else
                {
                    MessageBox.Show(
                    @"This only works with gmail accounts.
Sorry!",
                    "Not supported e-mail",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                    return;
                }
            }
            else
            {
                if (textBoxGoogleChatUsername.Text != "")
                {
                    email = textBoxGoogleChatUsername.Text += "@gmail.com";
                }
                else
                {
                    MessageBox.Show(
                    @"Please fill the textboxes before.",
                    "Not supported e-mail",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                    return;
                }
            }

            Jid googleChatUser = new Jid(email);

            googleChat.Username = googleChatUser.User;

            googleChat.Server = googleChatUser.Server;

            googleChat.Password = passwordBoxGoogleChat.Password;
            googleChat.AutoResolveConnectServer = true;

            googleChat.Open();

            textBoxGoogleChatUsername.IsEnabled = false;
            passwordBoxGoogleChat.IsEnabled = false;

            buttonGoogleChatSignIn.IsEnabled = false;
            checkBoxGoogleChatRememberMe.IsEnabled = false;
            buttonGoogleChatSignOut.IsEnabled = true;

            expanderGoogleChat.IsExpanded = true;
            expanderGoogleChatSignIn.IsExpanded = false;

            if (checkBoxGoogleChatRememberMe.IsChecked == true)
            {
                Properties.Settings.Default.password = passwordBoxGoogleChat.Password;
                Properties.Settings.Default.email = textBoxGoogleChatUsername.Text;

                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.password = "";
                Properties.Settings.Default.email = "";

                Properties.Settings.Default.Save();
            }
        }

        private void buttonSignOut_Click(object sender, RoutedEventArgs e)
        {
            // close the xmpp connection
            googleChat.Close();

            textBoxGoogleChatUsername.IsEnabled = true;
            passwordBoxGoogleChat.IsEnabled = true;

            buttonGoogleChatSignIn.IsEnabled = true;
            checkBoxGoogleChatRememberMe.IsEnabled = true;
            buttonGoogleChatSignOut.IsEnabled = false;

            expanderGoogleChat.IsExpanded = false;

        }
        #endregion


        #region Google Chat
        private void googleChat_OnClose(object sender)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                listGoogleChatEvents.Items.Add("OnClose Connection closed");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnError(object sender, Exception ex)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                listGoogleChatEvents.Items.Add("OnError");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnAuthError(object sender, agsXMPP.Xml.Dom.Element e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                listGoogleChatEvents.Items.Add("OnAuthError");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnRosterItem(object sender, agsXMPP.protocol.iq.roster.RosterItem item)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                //listGoogleChatEvents.Items.Add(String.Format("Received Contact {0}", item.Jid.Bare));
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));

        }

        private void googleChat_OnRosterEnd(object sender)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                listGoogleChatEvents.Items.Add("OnRosterEnd");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));

            // Send our own presence to teh server, so other epople send us online
            // and the server sends us the presences of our contacts when they are
            // available
            googleChat.SendMyPresence();
        }

        private void googleChat_OnRosterStart(object sender)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                listGoogleChatEvents.Items.Add("OnRosterStart");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnLogin(object sender)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                listGoogleChatEvents.Items.Add("OnLogin");
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnMessage(object sender, agsXMPP.protocol.client.Message msg)
        {
            // ignore empty messages (events)
            if (msg.Body == null || msg.From.Bare == "")
                return;

            _dispatcher.BeginInvoke((Action)(() =>
            {
                listGoogleChatEvents.Items.Add(String.Format("Message from {1}: {2}", msg.From.User, msg.From.Bare, msg.Body));
                listGoogleChatEvents.SelectedIndex = listGoogleChatEvents.Items.Count - 1;
            }));
        }

        private void googleChat_OnPresence(object sender, agsXMPP.protocol.client.Presence pres)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                String email = pres.From.ToString();
                //email = pres.From.Bare;

                int sameItemNumber = 0;
                int i = 0;
                while (i < comboBoxSelectedUser.Items.Count)
                {
                    if (email == comboBoxSelectedUser.Items.GetItemAt(i).ToString())
                        sameItemNumber++;

                    i++;
                }

                if (sameItemNumber == 0)
                {
                    comboBoxSelectedUser.Items.Add(email);
                }

                //listGoogleChatEvents.Items.Add(String.Format("Received Presence from:{0} user:{1} server:{2}", pres.From.Bare, pres.From.User, pres.From.Server));// .Status));

            }));
        }
        #endregion


        private void updateKeyboard(object sender, KeyEventArgs e)
        {
            if (comboBoxSelectedUser.Text != "" && comboBoxSelectedUser.Text != null)
            {
                String msg = "";

                if (e.Key == Key.Left)
                {
                    labelOrderValue.Content = "a - left";
                    msg = "a";
                }
                else if (e.Key == Key.Right)
                {
                    labelOrderValue.Content = "d - right";
                    msg = "d";
                }
                else if (e.Key == Key.Up)
                {
                    labelOrderValue.Content = "w - up";
                    msg = "w";
                }
                else if (e.Key == Key.Down)
                {
                    labelOrderValue.Content = "s - down";
                    msg = "s";
                }
                else if (e.Key == Key.Space)
                {
                    labelOrderValue.Content = "p - stop";
                    msg = "p";
                }
                
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    if (msg != "")
                    {
                        // Send a message
                        agsXMPP.protocol.client.Message chatMsg = new agsXMPP.protocol.client.Message();
                        chatMsg.Type = agsXMPP.protocol.client.MessageType.chat;
                        //msg.To = new Jid(txtJabberIdReceiver.Text);
                        chatMsg.To = new Jid(comboBoxSelectedUser.SelectedItem.ToString());
                        chatMsg.Body = msg;

                        googleChat.Send(chatMsg);
                    }
                
                    lastMessageSent = msg;
                    lastMessageSender = "keyboard";

                }));
            }
            else
            {
                labelOrderValue.Content = "No user selected";
            }
        }


        private void updateController(object sender, EventArgs e)
        {
            // Get the game pad state.
            GamePadState currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.IsConnected)
            {
                // Allows the game to exit
                if (currentState.Buttons.Back == ButtonState.Pressed)
                    this.Close();

                String msg = "";
                String msgThumbstick = "";
                String msgButtons = "";

                float xValue = 0.0f;
                float yValue = 0.0f;

                // Rotate the model using the left thumbstick, and scale it down
                xValue = currentState.ThumbSticks.Left.X * 0.10f;
                yValue = currentState.ThumbSticks.Left.Y * 0.10f;

                if (xValue < -0.09)
                    msgThumbstick = "a";
                else if (xValue > 0.09)
                    msgThumbstick = "d";
                else if (yValue < -0.09)
                    msgThumbstick = "s";
                else if (yValue > 0.09)
                    msgThumbstick = "w";
                else
                    msgThumbstick = "p";

                if (currentState.DPad.Left == ButtonState.Pressed)
                    msgButtons = "a";
                else if (currentState.DPad.Right == ButtonState.Pressed)
                    msgButtons = "d";
                else if (currentState.DPad.Up == ButtonState.Pressed)
                    msgButtons = "w";
                else if (currentState.DPad.Down == ButtonState.Pressed)
                    msgButtons = "s";
                else
                    msgButtons = "p";


                if (comboBoxSelectedUser.Text != "" && comboBoxSelectedUser.Text != null)
                {
                    if (msgThumbstick == "p" && msgButtons == "p" && lastMessageSender == "controller")
                    {
                        labelOrderValue.Content = "p - stop";
                        msg = "p";
                    }
                    else if (msgThumbstick == "a" || msgButtons == "a")
                    {
                        labelOrderValue.Content = "a - left";
                        msg = "a";
                    }
                    else if (msgThumbstick == "d" || msgButtons == "d")
                    {
                        labelOrderValue.Content = "d - right";
                        msg = "d";
                    }
                    else if (msgThumbstick == "w" || msgButtons == "w")
                    {
                        labelOrderValue.Content = "w - up";
                        msg = "w";
                    }
                    else if (msgThumbstick == "s" || msgButtons == "s")
                    {
                        labelOrderValue.Content = "s - down";
                        msg = "s";
                    }

                    GamePad.SetVibration(PlayerIndex.One,
                        currentState.Triggers.Right,
                        currentState.Triggers.Right);

                    _dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (comboBoxSelectedUser.Text != "" && msg != "" && msg != lastMessageSent)
                        {
                            // Send a message
                            agsXMPP.protocol.client.Message chatMsg = new agsXMPP.protocol.client.Message();
                            chatMsg.Type = agsXMPP.protocol.client.MessageType.chat;
                            //msg.To = new Jid(txtJabberIdReceiver.Text);
                            chatMsg.To = new Jid(comboBoxSelectedUser.SelectedItem.ToString());
                            chatMsg.Body = msg;

                            googleChat.Send(chatMsg);

                            lastMessageSent = msg;
                            lastMessageSender = "controller";
                        }
                    }));
                }
                else
                {
                    labelOrderValue.Content = "No user selected";
                }
            }
        }

        #region Expander Events

        private void manageCollapses(object sender, RoutedEventArgs e)
        {
            try
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    int rightHeight = 64;
                    if (expanderGoogleChatSignIn.IsExpanded == true)
                        rightHeight += (int)expanderGoogleChatSignIn.Height;
                    else
                        rightHeight += 32;

                    Canvas.SetTop(expanderGoogleChat, rightHeight - 46);

                    if (expanderGoogleChat.IsExpanded == true)
                        rightHeight += (int)expanderGoogleChat.Height;
                    else
                        rightHeight += 32;

                    googleChatRemoteController.Height = rightHeight;
                }));
            }
            catch
            {
            }
        }
        #endregion

        private void comboBoxSelectedUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                labelSendingOrdersToValue.Content = comboBoxSelectedUser.SelectedItem.ToString();

                buttonExpand.IsEnabled = true;

                expanderGoogleChat.IsEnabled = false;
                expanderGoogleChatSignIn.IsEnabled = false;

                googleChatRemoteController.Height = 200;
                googleChatRemoteController.Width = 200;

                buttonExpand.Content = "Expand >>>";

                expanded = false;
            }));
        }

        private void buttonExpand_Click(object sender, RoutedEventArgs e)
        {
            _dispatcher.BeginInvoke((Action)(() =>
            {
                if (expanded)
                {
                    expanderGoogleChat.IsEnabled = false;
                    expanderGoogleChatSignIn.IsEnabled = false;

                    googleChatRemoteController.Height = 200;
                    googleChatRemoteController.Width = 200;

                    buttonExpand.Content = "Expand >>>";

                    expanded = false;
                }
                else
                {
                    googleChatRemoteController.Height = 520;
                    googleChatRemoteController.Width = 660;

                    expanderGoogleChat.IsEnabled = true;
                    expanderGoogleChatSignIn.IsEnabled = true;

                    expanderGoogleChat.IsExpanded = true;
                    expanderGoogleChatSignIn.IsExpanded = true;

                    buttonExpand.Content = "<<< Return";

                    expanded = true;
                }
            }));
        }
    }


}
