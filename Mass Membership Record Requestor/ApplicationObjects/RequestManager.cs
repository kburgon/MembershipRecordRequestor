using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace Mass_Membership_Record_Requestor.ApplicationObjects
{
    public class RequestManager : INotifyPropertyChanged
    {
        private List<MembershipRecordInfo> _membersInQueue;
        public List<MembershipRecordInfo> MembersInQueue
        {
            get
            {
                return _membersInQueue;
            }
            set
            {
                _membersInQueue = value;
                PropertyHasChanged("MembersInQueue");
            }
        }

        private List<RecordRequestor> _registeredRequestors;
        private Mutex _queueMutex;

        public event PropertyChangedEventHandler PropertyChanged;

        public RequestManager()
        {
            _membersInQueue = new List<MembershipRecordInfo> { };
            _registeredRequestors = new List<RecordRequestor> { };
            _queueMutex = new Mutex();
        }

        private void PropertyHasChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        /// <summary>
        /// Adds requestors to the list of registered requestors.
        /// </summary>
        /// <param name="requestorToRegister"> The requestor to add to the list of registered requestors. </param>
        public void RegisterRequestor(RecordRequestor requestorToRegister)
        {
            requestorToRegister.SubscribeToHost(this);
            _registeredRequestors.Add(requestorToRegister);
        }

        /// <summary>
        /// Removes requestors from the list of registered requestors.
        /// </summary>
        /// <param name="requestorToRemove"> The requestor to remove from the list of registered requestors. </param>
        public void RemoveRegisteredRequestor(RecordRequestor requestorToRemove)
        {
            requestorToRemove.UnsubscribeFromHost();
            _registeredRequestors.Remove(requestorToRemove);
        }

        /// <summary>
        /// Notifies the registered requestors that a request is available.
        /// </summary>
        private void BroadcastRequestsAvailable()
        {
            foreach (var requestor in _registeredRequestors)
            {
                requestor.ManagerHasRequestAvailable = true;
            }
        }

        /// <summary>
        /// Notifies the registered requestors that no requests are available.
        /// </summary>
        private void BroadcastNoRequestsAvailable()
        {
            foreach (var requestor in _registeredRequestors)
            {
                requestor.ManagerHasRequestAvailable = false;
            }
        }

        /// <summary>
        /// Add the membership info to the request queue.
        /// </summary>
        /// <param name="requestToAdd"> The request to add to the queue. </param>
        public void AddRequestToQueue(MembershipRecordInfo requestToAdd)
        {
            MembersInQueue.Add(requestToAdd);
            BroadcastRequestsAvailable();
        }

        /// <summary>
        /// Gets the next request from the queue.
        /// </summary>
        /// <returns> The next request in the queue. </returns>
        public MembershipRecordInfo GetNextRequest()
        {
            _queueMutex.WaitOne();
            var nextRequest = MembersInQueue.First();
            MembersInQueue.Remove(nextRequest);

            if (MembersInQueue.Count == 0)
            {
                BroadcastNoRequestsAvailable();
            }

            PropertyHasChanged("MembersInQueue");
            _queueMutex.ReleaseMutex();

            return nextRequest;
        }
    }
}
