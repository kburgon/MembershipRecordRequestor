using System.Windows;
using System.Windows.Input;
using Mass_Membership_Record_Requestor.ApplicationObjects;
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace Mass_Membership_Record_Requestor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private RequestManager _requestManager;
        private RecordRequestor m_mainRequestor;
        private List<MembershipRecordInfo> _membersToRequest;

        public List<MembershipRecordInfo> MembersToRequest
        {
            get
            {
                return _membersToRequest;
            }
            set
            {
                _membersToRequest = value;
                NotifyPropertyChanged("MembersToRequest");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            _requestManager = new RequestManager();
            m_mainRequestor = new RecordRequestor();
            _requestManager.RegisterRequestor(m_mainRequestor);

            PropertyChanged += MainWindow_PropertyChanged;
        }

        private void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RequestQueueListBox.ItemsSource = null;
            RequestQueueListBox.ItemsSource = _requestManager.MembersInQueue;
        }

        private void NotifyPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Creates a request with all the data in the membership info section of the UI and sends it to the request manager.
        /// </summary>
        private void SendRecordRequest()
        {
            var request = new MembershipRecordInfo()
            {
                Name = NameTextBox.Text,
                BirthDate = BirthDateDatePicker.SelectedDate.Value.ToString("dd MMM yyyy"),
                PhoneNumber = PhoneNumberTextBox.Text,
                EmailAddress = EmailTextBox.Text,
                ServiceAddress1 = ServiceAddress1TextBox.Text,
                ServiceAddress2 = ServiceAddress2TextBox.Text
            };

            _requestManager.AddRequestToQueue(request);
            RequestQueueListBox.ItemsSource = _requestManager.MembersInQueue;
            ClearMembershipInfoInputFields();
        }

        /// <summary>
        /// Clears the input fields for entering membership info.
        /// </summary>
        private void ClearMembershipInfoInputFields()
        {
            NameTextBox.Clear();
            BirthDateDatePicker.SelectedDate = null;
            BirthDateDatePicker.DisplayDate = DateTime.Today;
            PhoneNumberTextBox.Clear();
            EmailTextBox.Clear();
            ServiceAddress1TextBox.Clear();
            ServiceAddress2TextBox.Clear();
        }

        private void ServiceAddressTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SendRecordRequest();
                RequestQueueListBox.ItemsSource = _requestManager.MembersInQueue;
                NameTextBox.Focus();
            }
        }

        private void AddToQueueButton_Click(object sender, RoutedEventArgs e)
        {
            SendRecordRequest();
        }

        private void StartChromeButton_Click(object sender, RoutedEventArgs e)
        {
            m_mainRequestor.StartRequestor();
        }

        private void CloseChromeButton_Click(object sender, RoutedEventArgs e)
        {
            m_mainRequestor.Dispose();
        }
    }
}
