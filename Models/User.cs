using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TibiaMarche_WebApp.Models
{
    public class User
    {
        private readonly string _id;    // Firebase Authentication UID (never changes)
        private string _email;          // unique, acts as username

        protected User(string id, string email)
        {
            _id = id;
            _email = email;
        }

        public string Id
        {
            get { return _id; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        // todo Authentication functions

        // todo class methods
    }
}