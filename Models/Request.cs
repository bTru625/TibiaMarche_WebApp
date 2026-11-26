using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TibiaMarche_WebApp.Models
{
    public class Request
    {
        public enum RequestStatus { OPEN, CLOSED, FULFILLED }

        private string? _id;
        private string _itemName = string.Empty;
        private Server _server = null!;
        private string? _buyerId;
        private DateTime _createdAt;
        private RequestStatus _status = RequestStatus.OPEN;
        private string? _notifyEmail;

        // Parameterless constructor (model binding / Firestore)
        public Request()
        {
        }

        public Request(string itemName, Server server, string? buyerId)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                throw new ArgumentException("Item name is required.");
            }
            if (server == null)
            {
                throw new ArgumentException("Server is required.");
            }
            _itemName = itemName.Trim();
            _server = server;
            _buyerId = buyerId;
            _createdAt = DateTime.Now;
        }

        public string? Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string ItemName
        {
            get { return _itemName; }
            set { _itemName = value; }
        }

        public Server Server
        {
            get { return _server; }
            set { _server = value; }
        }

        public string? BuyerId
        {
            get { return _buyerId; }
            set { _buyerId = value; }
        }

        public DateTime CreatedAt
        {
            get { return _createdAt; }
            set { _createdAt = value; }
        }

        // Where to tell the buyer the item is back (account email or an alternative).
        // TODO(J): add notifyEmail to docs/data-model.md so Android writes the same field.
        public string? NotifyEmail
        {
            get { return _notifyEmail; }
            set { _notifyEmail = value; }
        }

        public RequestStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public void MarkFulfilled()
        {
            _status = RequestStatus.FULFILLED;
        }

        public void MarkClosed()
        {
            _status = RequestStatus.CLOSED;
        }

        public void MarkOpen()
        {
            _status = RequestStatus.OPEN;
        }
    }
}